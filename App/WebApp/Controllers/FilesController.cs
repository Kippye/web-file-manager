using Application.Contracts;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers
{
    // [RequestSizeLimit(...)]
    [Authorize]
    [RequestFormLimits(MultipartBodyLengthLimit = 134217728, ValueCountLimit = 3)]
    public class FilesController(
        IFileEncryptionService fileEncryptionService,
        IFileStorageService fileStorageService,
        ILogger<HomeController> logger
    ) : Controller
    {
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
                return View(vm);
            }

            // TODO: Initial validation: size, type, extension, etc.

            Result<EncryptedFile>? encryptionResult = null;
            using (var formFileStream = vm.File.OpenReadStream())
            {
                var fileBytes = new byte[formFileStream.Length];
                // TODO: Handle end-of-stream exception
                await formFileStream.ReadExactlyAsync(fileBytes);
                encryptionResult = await fileEncryptionService.EncryptAsync(fileBytes);
            }
            if (encryptionResult is null)
            {
                logger.LogError("Failed to read uploaded file.");
                return View(vm);
            }
            if (!encryptionResult.IsSuccess)
            {
                logger.LogError(string.Join(". ", encryptionResult.Errors.Select(e => e.Message)));
                return View(vm);
            }

            var fileUploadInfo = new FileUploadInfoDto()
            {
                FileName = vm.File.FileName,
                ContentType = vm.File.ContentType,
                FileSize = vm.File.Length,
            };
            var storeEncryptedResult = await fileStorageService.StoreFileAsync(fileUploadInfo, encryptionResult.Value!);
            if (!storeEncryptedResult.IsSuccess)
            {
                logger.LogError(string.Join(". ", storeEncryptedResult.Errors.Select(e => e.Message)));
                return BadRequest(); // TODO: Return correct result; show error messages
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