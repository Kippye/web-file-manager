using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class FileUploadViewModel
{
    [Required]
    public List<IFormFile> Files { get; set; } = default!;
}