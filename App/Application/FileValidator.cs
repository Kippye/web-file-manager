using Application.Base;
using Application.Contracts;
using DTO;
using Infrastructure.Contracts;

namespace Application;

public class FileValidator(IAppLogger<FileValidator> logger) : BaseFileValidator, IFileValidator
{
    public Result Validate(FileUploadInfoDto fileInfo)
    {
        // NOTE: Storing and then loading empty content type causes a 500 error due to invalid header value
        if (string.IsNullOrWhiteSpace(fileInfo.ContentType))
        {
            logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' has empty content type '{FileContentType}'", fileInfo.FileName, fileInfo.ContentType);
            return Result.Failure(EApplicationErrorCode.ValidationError, "Invalid file type");
        }

        string extension = GetFileExtension(fileInfo.FileName);

        string? allowingPattern = GetMatchingContentType(fileInfo.ContentType, hasExtension: !string.IsNullOrEmpty(extension));
        if (allowingPattern is null)
        {
            logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' has unallowed content type '{FileContentType}'", fileInfo.FileName, fileInfo.ContentType);
            return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file content type");
        }

        (string category, string type) = GetContentTypeSplit(fileInfo.ContentType);

        // We allow files with no extension
        if (!string.IsNullOrEmpty(extension))
        {
            if (!AllowedExtensions.ContainsKey(category))
            {
                logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' content type category '{Category}' not found in allowed extensions dictionary", fileInfo.FileName, category);
                return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }

            FileExtensionSet extensionSet = AllowedExtensions[category];
            if (!extensionSet.Contains(extension))
            {
                logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' has unallowed extension '{FileExtension}'", fileInfo.FileName, extension);
                return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }

            // Check that the extension maps to the same content type as the file has
            // NOTE: These only log warnings because the mapping isn't accurate enough to be considered a failure
            // e.g. it maps .cpp files to text/plain
            string? mappedContentType = MapFileNameToContentType(fileInfo.FileName);
            if (mappedContentType is null)
            {
                logger.LogWarning(ValidateFileEvent, "File '{FileName}' extension '{FileExtension}' could not be mapped to a content type", fileInfo.FileName, extension);
                // return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }
            else if (!mappedContentType.Equals(fileInfo.ContentType))
            {
                logger.LogWarning(
                    ValidateFileEvent,
                    "File '{FileName}' has content type '{FileContentType}' but extension '{FileExtension}' maps to '{MappedContentType}'",
                    fileInfo.FileName,
                    fileInfo.ContentType,
                    extension,
                    mappedContentType
                );
                // return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }
        }

        logger.LogInformation(ValidateFileEvent, EventOutcome.Success, "File '{FileName}' with extension '{FileExtension}' passes allowed content type pattern '{ContentTypePattern}'", fileInfo.FileName, extension, allowingPattern);
        return Result.Success();
    }
}
