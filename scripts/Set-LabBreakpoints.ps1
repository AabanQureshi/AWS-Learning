[CmdletBinding()]
param(
    [ValidateSet('All', 'Configuration', 'Upload', 'Worker', 'Read', 'Failures', 'Duplicate', 'None')]
    [string] $Phase = 'All',
    [switch] $ListOnly
)

$ErrorActionPreference = 'Stop'
$solutionRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $solutionRoot 'AWS-Learning.slnx'
$definitions = Get-Content -LiteralPath (Join-Path $solutionRoot 'debugging/day-1-breakpoints.json') -Raw | ConvertFrom-Json

# Resolve anchors against saved source, so line-number changes do not require editing this plan.
$resolved = foreach ($definition in $definitions) {
    $sourcePath = [IO.Path]::GetFullPath((Join-Path $solutionRoot $definition.File))
    if (-not $sourcePath.StartsWith($solutionRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'A breakpoint path is outside this solution.'
    }
    $lines = [IO.File]::ReadAllLines($sourcePath)
    $methodIndex = -1
    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Contains($definition.Method)) { $methodIndex = $index; break }
    }
    if ($methodIndex -lt 0) { throw "Method marker not found for $($definition.Id): $($definition.Method)" }
    $lineNumber = 0
    for ($index = $methodIndex; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Contains($definition.Anchor)) { $lineNumber = $index + 1; break }
        if ($index -gt $methodIndex -and $lines[$index] -match '^\s*(public|private|protected)\s+(static\s+)?(async\s+)?(Task|void|IActionResult|AWSCredentials)') { break }
    }
    if ($lineNumber -eq 0) { throw "Statement not found for $($definition.Id): $($definition.Anchor)" }
    $enabled = ($Phase -eq $definition.Phase) -or ($Phase -eq 'All' -and $definition.Phase -ne 'Configuration')
    if ($Phase -eq 'Duplicate' -and $definition.Id -eq 'W8') { $enabled = $true }
    [PSCustomObject]@{
        Id = $definition.Id; Phase = $definition.Phase; File = $sourcePath
        Line = $lineNumber; Condition = [string]$definition.Condition
        Enabled = $enabled; Inspect = $definition.Inspect
    }
}

if ($ListOnly) {
    $resolved | Select-Object Id, Phase, Line, Enabled, File, Inspect
    return
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'Visual Studio automation requires Windows. Use -ListOnly to inspect the plan.'
}

if (-not ('LearningVisualStudioInstances' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public static class LearningVisualStudioInstances
{
    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);
    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx context);

    public static object[] Find()
    {
        var found = new List<object>();
        IRunningObjectTable table;
        IBindCtx context;
        Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out table));
        Marshal.ThrowExceptionForHR(CreateBindCtx(0, out context));
        IEnumMoniker enumerator;
        table.EnumRunning(out enumerator);
        var monikers = new IMoniker[1];
        while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
        {
            string name;
            monikers[0].GetDisplayName(context, null, out name);
            if (name.StartsWith("!VisualStudio.DTE", StringComparison.OrdinalIgnoreCase))
            {
                object instance;
                table.GetObject(monikers[0], out instance);
                found.Add(instance);
            }
            Marshal.ReleaseComObject(monikers[0]);
        }
        Marshal.ReleaseComObject(enumerator);
        Marshal.ReleaseComObject(context);
        Marshal.ReleaseComObject(table);
        return found.ToArray();
    }
}
'@
}

$dte = $null
foreach ($instance in [LearningVisualStudioInstances]::Find()) {
    if ([string]::Equals($instance.Solution.FullName, $solutionPath, [StringComparison]::OrdinalIgnoreCase)) {
        $dte = $instance
        break
    }
}
if ($null -eq $dte) { throw 'Open AWS-Learning.slnx in Visual Studio first, using the same Windows account and elevation level as this terminal.' }

function Invoke-VisualStudioCall([scriptblock] $Operation) {
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try { return & $Operation }
        catch {
            if ($_.Exception.HResult -ne -2147418111 -or $attempt -eq 9) { throw }
            Start-Sleep -Milliseconds 200
        }
    }
}

$verified = foreach ($point in $resolved) {
    $breakpoint = $null
    foreach ($existing in $dte.Debugger.Breakpoints) {
        if ([string]::Equals($existing.File, $point.File, [StringComparison]::OrdinalIgnoreCase) -and
            [int]$existing.FileLine -eq $point.Line) {
            $breakpoint = $existing
            break
        }
    }
    if ($null -eq $breakpoint) {
        $added = Invoke-VisualStudioCall {
            $dte.Debugger.Breakpoints.Add('', $point.File, $point.Line, 1, $point.Condition, 1, 'C#', '', 1, '', 0, 1)
        }
        # PowerShell enumerates the returned COM Breakpoints collection.
        $breakpoint = @($added)[0]
    }
    Invoke-VisualStudioCall { $breakpoint.Enabled = $point.Enabled }
    if ([bool]$breakpoint.Enabled -ne [bool]$point.Enabled) { throw "Could not verify breakpoint $($point.Id)." }
    [PSCustomObject]@{
        Id = $point.Id; Phase = $point.Phase; Line = [int]$breakpoint.FileLine
        Enabled = [bool]$breakpoint.Enabled; File = [string]$breakpoint.File
        Condition = [string]$breakpoint.Condition
    }
}

$enabledCount = @($verified | Where-Object Enabled).Count
Write-Output "Verified $($verified.Count) teaching breakpoints in Visual Studio; $enabledCount enabled for phase '$Phase'."
$verified | Format-Table Id, Phase, Line, Enabled -AutoSize
