using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class FileUploadViewModel
{
    [Required]
    public IFormFile File { get; set; } = default!;
}