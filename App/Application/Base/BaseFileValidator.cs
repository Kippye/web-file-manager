using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Application.Base;

public abstract class BaseFileValidator
{
    protected static readonly EventId ValidateFileEvent = new(100, "File validated");

    /// <summary>
    /// Allowed / accepted file extensions, without the first period (e.g. 'png').
    /// </summary>
    protected readonly static HashSet<string> AllowedExtensions = [
        "png", "jpg", "jpeg", "webp", "gif", // Images
        "mp3", // Audio
        "mp4", // Video
        "txt", "pdf", "doc", "docx" // Documents
    ];

    /// <summary>
    /// Allowed / accepted values for ContentType of a file.
    /// Wildcards with * will accept any value.
    /// </summary>
    protected readonly static HashSet<string> AllowedContentTypes = [
        "image/*", // Images
        "audio/*", // Audio
        "video/*", // Video
        "text/*", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" // Documents
    ];

    protected string? MapFileNameToContentType(string fileName)
    {
        if (new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string mappedContentType))
        {
            return mappedContentType;
        }
        return null;
    }

    protected string? GetAllowingContentTypePattern(string contentType, bool hasExtension)
    {
        if (AllowedContentTypes.Contains(contentType))
        {
            return contentType;
        }

        var category = contentType.Split('/')[0];
        var wildcardContentType = category + "/*";
        if (AllowedContentTypes.Contains(wildcardContentType))
        {
            return wildcardContentType;
        }

        // Extensionless files may be sent as application/octet-stream
        if (hasExtension == false && contentType == "application/octet-stream")
        {
            return "application/octet-stream";
        }

        return null;
    }
}
