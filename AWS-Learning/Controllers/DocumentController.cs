using AWS_Learning.Filters;
using AWS_Learning.Models;
using AWS_Learning.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AWS_Learning.Controllers;

[Route("documents")]
[TypeFilter(typeof(LabExceptionFilter))]
public sealed class DocumentController(IDocumentService documents) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("")]
    public IActionResult Index() => View(new DocumentIndexViewModel());

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        DocumentIndexViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", model);
        }

        try
        {
            await using var content = model.File.OpenReadStream();
            var uploaded = await documents.UploadAsync(
                content, model.File.FileName, model.File.Length, cancellationToken);
            return RedirectToAction(nameof(Details), new { jobId = uploaded.DocumentId });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(model.File), exception.Message);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", model);
        }
    }

    [HttpGet("find")]
    public IActionResult Find(string? jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            ModelState.AddModelError(string.Empty, "Enter a job ID to look up its status.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", new DocumentIndexViewModel());
        }

        return RedirectToAction(nameof(Details), new { jobId = jobId.Trim() });
    }

    [HttpGet("{jobId}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Details(string jobId, CancellationToken cancellationToken)
    {
        var job = await documents.GetJobAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException("Job not found.");
        return View(new DocumentDetailsViewModel(job));
    }

    [HttpGet("{jobId}/status")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Status(string jobId, CancellationToken cancellationToken)
    {
        var job = await documents.GetJobAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException("Job not found.");
        return Json(job);
    }

    [HttpGet("{jobId}/result")]
    public async Task<IActionResult> Result(string jobId, CancellationToken cancellationToken)
    {
        var document = await documents.GetResultAsync(jobId, cancellationToken);
        await using var content = document.Content;
        using var reader = new StreamReader(content, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var statistics = JsonSerializer.Deserialize<DocumentProcessingResult>(json, JsonOptions)
            ?? throw new JsonException("Result is empty.");
        if (statistics.JobId != jobId)
        {
            throw new JsonException("Result does not belong to the requested job.");
        }

        return View(new DocumentResultViewModel(jobId, statistics, json));
    }

    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> Download(string jobId, CancellationToken cancellationToken)
    {
        var document = await documents.GetResultAsync(jobId, cancellationToken);
        // FileStreamResult owns and disposes the stream after the response finishes.
        return File(document.Content, document.ContentType, document.FileName);
    }
}
