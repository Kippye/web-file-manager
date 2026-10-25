using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Application.Base;

public abstract class BaseFileValidator
{
    protected static readonly EventId ValidateFileEvent = new(100, "File validated");

    protected static readonly Dictionary<string, string> ExtraTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        {".odt", "application/vnd.oasis.opendocument.text"},
        {".odp", "application/vnd.oasis.opendocument.presentation"},
        {".ods", "application/vnd.oasis.opendocument.spreadsheet"}
    };

    // TODO: Maybe categorise here so i can get the category of a file 
    // easier than checking all the application-specific types?
    /// <summary>
    /// Allowed / accepted file extensions, without the first period (e.g. 'png').
    /// </summary>
    protected readonly static HashSet<string> AllowedExtensions = [
        "png", "jpg", "jpeg", "webp", "gif", // Images
        "mp3", // Audio
        "mp4", // Video
        "doc", "docx", "odt", "txt", "pdf", // Documents
        "ppt", "pptx", "odp", // Presentations
        "xls", "xlsx", "ods" // Spreadsheets
    ];

    /// <summary>
    /// Allowed / accepted values for ContentType of a file.
    /// Wildcards with * will accept any value.
    /// </summary>
    protected readonly static HashSet<string> AllowedContentTypes = [
        "image/*", // Images
        "audio/*", // Audio
        "video/*", // Video
        "text/*", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.oasis.opendocument.text", // Documents
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/vnd.oasis.opendocument.presentation", // Presentations
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.oasis.opendocument.spreadsheet"
    ];

    protected FileExtensionContentTypeProvider ExtensionContentTypeProvider;

    public BaseFileValidator()
    {
        var mergedMappings = new FileExtensionContentTypeProvider().Mappings;
        ExtraTypeMappings
            .ToList()
            .ForEach(entry => mergedMappings[entry.Key] = entry.Value);
        ExtensionContentTypeProvider = new FileExtensionContentTypeProvider(mergedMappings);
    }

    protected string? MapFileNameToContentType(string fileName)
    {
        if (ExtensionContentTypeProvider.TryGetContentType(fileName, out string mappedContentType))
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
