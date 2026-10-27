[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'AWSLearning.Day5.Lambda.csproj'
$repoPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$artifactPath = Join-Path $repoPath 'artifacts/day-5'
# A fresh publish directory prevents obsolete assemblies entering later ZIPs.
$publishPath = Join-Path $artifactPath ('publish-' + [guid]::NewGuid().ToString('N'))
$zipPath = Join-Path $artifactPath 'function.zip'

dotnet publish $projectPath --configuration Release --runtime linux-x64 --self-contained false -p:PublishReadyToRun=false --output $publishPath
if ($LASTEXITCODE -ne 0) { throw 'Lambda publish failed.' }

Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $zipPath -Force
Write-Output "Lambda package: $zipPath"
