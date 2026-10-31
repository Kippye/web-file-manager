using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Application.Base;

public class FileExtensionSet
{
    public HashSet<string> Extensions { get; protected set; }
    public bool IsWildcard { get; protected set; }

    public bool Contains(string extension)
    {
        if (IsWildcard) { return true; }

        string otherForm;

        if (extension.StartsWith('.'))
        {
            otherForm = extension.TrimStart('.');
        }
        else
        {
            otherForm = "." + extension;
        }

        return Extensions.Contains(extension) || Extensions.Contains(otherForm);
    }

    public FileExtensionSet(HashSet<string> extensions)
    {
        Extensions = extensions;
    }

    /// <summary>
    /// A wildcard (*) extension set that 'contains' any extension.
    /// </summary>
    public static FileExtensionSet Wildcard() => new([]) { IsWildcard = true };
}

public enum FileCategory
{
    /// <summary>
    /// The default category for files that don't fit under any other category
    /// </summary>
    File,
    Archive,
    Audio,
    Binary,
    Code,
    Document,
    Image,
    PDF,
    Presentation,
    Spreadsheet,
    Text,
    Video
}

public abstract class BaseFileValidator
{
    protected FileExtensionContentTypeProvider ExtensionContentTypeProvider;
    protected static readonly EventId ValidateFileEvent = new(100, "File validated");

    protected static readonly Dictionary<string, string> ExtraTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        {".odt", "application/vnd.oasis.opendocument.text"},
        {".odp", "application/vnd.oasis.opendocument.presentation"},
        {".ods", "application/vnd.oasis.opendocument.spreadsheet"}
    };

    public readonly static Dictionary<FileCategory, (HashSet<string> Extensions, HashSet<string> Types)> FileCategories = new()
    {
        {FileCategory.Archive, ([], [])},
        {FileCategory.Audio, (Extensions: [], Types: ["audio/*"])},
        {FileCategory.Binary, ([], [])},
        {FileCategory.Code, (Extensions: ["c", "cs", "cpp", "cxx", "h", "hpp", "java", "js", "json", "ts", "php", "py"], Types: ["text/html"])},
        {FileCategory.Document, (
            Extensions: [],
            Types: ["application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.oasis.opendocument.text"]
        )},
        {FileCategory.Image, (Extensions: [], Types: ["image/*"])},
        {FileCategory.PDF, (Extensions: ["pdf"], Types: ["application/pdf"])},
        {FileCategory.Presentation, (
            Extensions: [],
            Types: ["application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/vnd.oasis.opendocument.presentation"]
        )},
        {FileCategory.Spreadsheet, (
            Extensions: [],
            Types: ["application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.oasis.opendocument.spreadsheet"]
        )},
        {FileCategory.Text, (Extensions: [], Types: ["text/*"])},
        {FileCategory.Video, (Extensions: [], Types: ["video/*"])},
    };

    /// <summary>
    /// Allowed / accepted file extensions by content type category.
    /// </summary>
    public readonly static Dictionary<string, FileExtensionSet> AllowedExtensions = new()
    {
        { "image", FileExtensionSet.Wildcard()},
        { "audio", FileExtensionSet.Wildcard() },
        { "video", FileExtensionSet.Wildcard() },
        { "text", FileExtensionSet.Wildcard() },
        { "application", new FileExtensionSet([
            "json", // Kinda text
            "doc", "docx", "odt", "pdf", // Documents
            "ppt", "pptx", "odp", // Presentations
            "xls", "xlsx", "ods" // Spreadsheets
            ])
        }
    };

    /// <summary>
    /// Allowed / accepted values for ContentType of a file.
    /// Wildcards with * will accept any value.
    /// </summary>
    public readonly static HashSet<string> AllowedContentTypes = [
        "image/*", // Images
        "audio/*", // Audio
        "video/*", // Video
        "text/*", "application/json", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.oasis.opendocument.text", // Documents
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/vnd.oasis.opendocument.presentation", // Presentations
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.oasis.opendocument.spreadsheet"
    ];


    public static (string category, string type) GetContentTypeSplit(string contentType)
    {
        var parts = contentType.Split('/');
        var category = parts[0];
        var type = parts.Length > 1 ? parts[1] : "";
        return (category, type);
    }

    public static string GetFileExtension(string filename, bool withPeriod = false)
    {
        string extension = Path.GetExtension(filename);
        // A filename like '.gitignore' should be accepted as extensionless text
        if (extension == filename)
        {
            return "";
        }
        // Other extensions get the leading . removed
        else
        {
            return withPeriod ? extension : extension.TrimStart('.');
        }
    }

    public static string CreateAcceptString()
    {
        StringBuilder sb = new();

        void AddEntry(string entry)
        {
            if (sb.Length != 0)
            {
                sb.Append(',');
            }
            sb.Append(entry);
        }

        foreach (string contentType in AllowedContentTypes)
        {
            (string category, string type) = GetContentTypeSplit(contentType);

            /* How this works:
                1. If the content type is not a wildcard, accept only the specific content type
                2. If the extension set for category is a wildcard, accept the whole category
                3. If the extension set for the category has extensions, accept all those extensions
            */
            if (type != "*")
            {
                AddEntry(contentType);
            }
            else
            {
                if (AllowedExtensions.ContainsKey(category))
                {
                    FileExtensionSet extensionSet = AllowedExtensions[category];

                    if (extensionSet.IsWildcard)
                    {
                        AddEntry(contentType);
                    }
                    foreach (string extension in extensionSet.Extensions)
                    {
                        AddEntry(extension.StartsWith('.') ? extension : "." + extension);
                    }
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get the category of a file based on its content type and extension. 
    /// Returns FileCategory.File if the file doesn't fit any other category.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="contentType"></param>
    /// <returns></returns>
    public static FileCategory GetFileCategory(string filename, string contentType)
    {
        (string category, _) = GetContentTypeSplit(contentType);
        string wildcardType = category + "/*";
        foreach (var entry in FileCategories)
        {
            // Does the category contain the content type or its wildcard?
            if (entry.Value.Types.Contains(contentType) || entry.Value.Types.Contains(wildcardType))
            {
                return entry.Key;
            }
            // Does the category contain the file's extension?
            if (entry.Value.Extensions.Contains(GetFileExtension(filename)))
            {
                return entry.Key;
            }
        }
        return FileCategory.File;
    }

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

    protected string? GetMatchingContentType(string contentType, bool hasExtension)
    {
        if (AllowedContentTypes.Contains(contentType))
        {
            return contentType;
        }

        (string category, _) = GetContentTypeSplit(contentType);
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
