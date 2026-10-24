using Application.Base;
using Application.Contracts;
using DTO;
using Infrastructure.Contracts;

namespace Application;

public class FileValidator(IAppLogger<FileValidator> logger) : BaseFileValidator, IFileValidator
{
    public Result Validate(FileUploadInfoDto fileInfo)
    {
        // Get extension (without '.')
        string extension = Path.GetExtension(fileInfo.FileName).TrimStart('.');

        // We allow files with no extension
        if (!string.IsNullOrEmpty(extension))
        {
            if (!AllowedExtensions.Contains(extension))
            {
                logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' has unallowed extension '{FileExtension}'", fileInfo.FileName, extension);
                return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }

            string? mappedContentType = MapFileNameToContentType(fileInfo.FileName);
            if (mappedContentType is null)
            {
                logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' extension '{FileExtension}' could not be mapped to a content type", fileInfo.FileName, extension);
                return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }
            if (!mappedContentType.Equals(fileInfo.ContentType))
            {
                logger.LogInformation(
                    ValidateFileEvent,
                    EventOutcome.Failure,
                    "File '{FileName}' has content type '{FileContentType}' but extension '{FileExtension}' maps to '{MappedContentType}'",
                    fileInfo.FileName,
                    fileInfo.ContentType,
                    extension,
                    mappedContentType
                );
                return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file extension '{extension}'");
            }
        }

        string? allowingPattern = GetAllowingContentTypePattern(fileInfo.ContentType, hasExtension: !string.IsNullOrEmpty(extension));
        if (allowingPattern is null)
        {
            logger.LogInformation(ValidateFileEvent, EventOutcome.Failure, "File '{FileName}' has unallowed content type '{FileContentType}'", fileInfo.FileName, fileInfo.ContentType);
            return Result.Failure(EApplicationErrorCode.ValidationError, $"Invalid file content type");
        }

        logger.LogInformation(ValidateFileEvent, EventOutcome.Success, "File '{FileName}' with extension '{FileExtension}' passes allowed content type pattern '{ContentTypePattern}'", fileInfo.FileName, extension, allowingPattern);
        return Result.Success();
    }
}
