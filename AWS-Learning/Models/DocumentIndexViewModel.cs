using System.ComponentModel.DataAnnotations;

namespace AWS_Learning.Models;

public sealed class DocumentIndexViewModel
{
    [Required(ErrorMessage = "Choose a text file to upload.")]
    public IFormFile? File { get; set; }
}
