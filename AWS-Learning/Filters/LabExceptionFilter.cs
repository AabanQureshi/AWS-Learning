using Amazon.Runtime;
using AWS_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;

namespace AWS_Learning.Filters;

public sealed class LabExceptionFilter(
    ILogger<LabExceptionFilter> logger,
    IModelMetadataProvider metadataProvider) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException &&
            context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var (status, title, message) = context.Exception switch
        {
            KeyNotFoundException => (404, "Job not found", "No job exists with that ID. Check the ID and try again."),
            ArgumentException => (400, "Check your request", "The request contains an invalid value."),
            AmazonServiceException or AmazonClientException or HttpRequestException =>
                (503, "Service connection failed", "Check the local AWS endpoint, credential profile, and configured resources, then retry."),
            InvalidOperationException => (409, "Result is not ready", "This job has no completed result yet. Return to its status page."),
            JsonException or FormatException or InvalidDataException =>
                (502, "Unexpected stored data", "The stored job or result does not match the application's data format."),
            _ => (500, "Something went wrong", "The request could not be completed. Check the application log using the request ID below.")
        };

        logger.LogError(context.Exception, "Document request failed with status {Status}; request {RequestId}",
            status, context.HttpContext.TraceIdentifier);

        if (context.HttpContext.Request.Path.Value?.EndsWith("/status", StringComparison.OrdinalIgnoreCase) == true ||
            context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = message,
                Extensions = { ["requestId"] = context.HttpContext.TraceIdentifier }
            }) { StatusCode = status };
        }
        else
        {
            var viewData = new ViewDataDictionary<DocumentNoticeViewModel>(metadataProvider, context.ModelState)
            {
                Model = new DocumentNoticeViewModel(title, message, context.HttpContext.TraceIdentifier)
            };
            context.Result = new ViewResult
            {
                ViewName = "Notice",
                StatusCode = status,
                ViewData = viewData
            };
        }

        context.ExceptionHandled = true;
    }
}
