using System.Reflection;
using Application.Contracts;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    [RequestSizeLimit(134217728)]
    [RequestFormLimits(MultipartBodyLengthLimit = 134217728, ValueCountLimit = 4)]
    public class FilesController(
        IFileEncryptionService fileEncryptionService,
        IFileStorageService fileStorageService,
        ILogger<HomeController> logger
    ) : Controller
    {
        private async Task<Result> ProcessUploadedFile(IFormFile file)
        {
            Result<EncryptedFile>? encryptionResult = null;
            using (var formFileStream = file.OpenReadStream())
            {
                var fileBytes = new byte[formFileStream.Length];
                // TODO: Handle end-of-stream exception
                await formFileStream.ReadExactlyAsync(fileBytes);
                encryptionResult = await fileEncryptionService.EncryptAsync(fileBytes);
            }
            if (!encryptionResult.IsSuccess)
            {
                logger.LogError(string.Join(". ", encryptionResult.Errors.Select(e => e.Message)));
                return Result.Failure(encryptionResult.Errors);
            }

            var fileUploadInfo = new FileUploadInfoDto()
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
            };
            var storeEncryptedResult = await fileStorageService.StoreFileAsync(fileUploadInfo, encryptionResult.Value!);
            if (!storeEncryptedResult.IsSuccess)
            {
                logger.LogError(string.Join(". ", storeEncryptedResult.Errors.Select(e => e.Message)));
                return Result.Failure(storeEncryptedResult.Errors);
            }

            return Result.Success();
        }

        // List current user's files
        public async Task<IActionResult> Index()
        {
            List<FileInfoDto> fileList = await fileStorageService.GetFileListAsync();

            return View(
                fileList.Select(f =>
                    new FileInfoViewModel()
                    {
                        Id = f.Id,
                        ContentType = f.ContentType,
                        FileName = f.FileName,
                        FileSize = f.FileSize,
                        UploadedAt = f.UploadedAt.ToLocalTime().ToString()
                    }
                )
            );
        }

        // Upload page
        public IActionResult Upload()
        {
            return View();
        }

        // Upload file(s) via form
        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var requestSizeLimitAttr = typeof(FilesController).GetCustomAttribute<RequestSizeLimitAttribute>(true)! as IRequestSizeLimitMetadata;
                var requestFormLimitsAttr = typeof(FilesController).GetCustomAttribute<RequestFormLimitsAttribute>(true)!;

                foreach (var entry in ModelState)
                {
                    if (string.IsNullOrEmpty(entry.Key) && entry.Value.Errors.Count > 0)
                    {
                        entry.Value.Errors.Clear();
                        entry.Value.Errors.Add(
                            new ModelError(
                                $"An upload can contain at most {Math.Max(0, requestFormLimitsAttr.ValueCountLimit - 1)} files and be {LongFileSizeExtensions.AsFileSize((long)requestSizeLimitAttr.MaxRequestBodySize!)} total."
                            )
                        );
                    }
                }
                return View(vm);
            }

            // TODO: Initial validation: size, type, extension, etc.
            // Validate all files before any are processed!

            foreach (IFormFile file in vm.Files)
            {
                var processResult = await ProcessUploadedFile(file);

                if (!processResult.IsSuccess)
                {
                    // TODO: Handle specific causes
                    return BadRequest();
                }
            }

            return RedirectToAction("Index");
        }

        // Download file by ID
        public async Task<IActionResult> Download([FromRoute] Guid id)
        {
            // TODO: Reduce possible file content copies here

            var getFileResult = await fileStorageService.GetFileAsync(id);
            if (!getFileResult.IsSuccess)
            {
                return NotFound();
            }

            var decryptResult = await fileEncryptionService.DecryptAsync(
                getFileResult.Value!.File
            );
            if (!decryptResult.IsSuccess)
            {
                logger.LogError(string.Join(". ", decryptResult.Errors.Select(e => e.Message)));
                // TODO: Return correct result
                return BadRequest();
            }

            var fileInfo = getFileResult.Value!.Info;
            return File(decryptResult.Value!, fileInfo.ContentType, fileInfo.FileName);
        }

        // Delete file by id
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var deleteResult = await fileStorageService.DeleteFileAsync(id);

            if (!deleteResult.IsSuccess)
            {
                // TODO: Return correct result
                return NotFound();
            }

            return RedirectToAction("Index");
        }
    }
}