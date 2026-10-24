using System.Reflection;
using Application.Contracts;
using DTO;
using Infrastructure.Contracts;
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
        IUserResolverService userResolverService,
        IFileValidator fileValidator,
        IFileEncryptionService fileEncryptionService,
        IFileStorageService fileStorageService
    ) : Controller
    {
        private async Task<Result<Guid>> ProcessUploadedFile(IFormFile file)
        {
            Guid userId = (Guid)userResolverService.GetUserGuid(User)!;

            Result<EncryptedFile>? encryptionResult = null;
            try
            {
                using var formFileStream = file.OpenReadStream();
                var fileBytes = new byte[formFileStream.Length];
                await formFileStream.ReadExactlyAsync(fileBytes);
                encryptionResult = await fileEncryptionService.EncryptAsync(fileBytes);
            }
            catch
            {
                return Result<Guid>.Failure(EApplicationErrorCode.UnknownError, "Failed to read uploaded file.");
            }
            if (!encryptionResult.IsSuccess)
            {
                return Result<Guid>.Failure(encryptionResult.Errors);
            }

            var fileUploadInfo = new FileUploadInfoDto()
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
            };
            var storeEncryptedResult = await fileStorageService.StoreFileAsync(
                fileUploadInfo,
                encryptionResult.Value!,
                userId
            );
            if (!storeEncryptedResult.IsSuccess)
            {
                return Result<Guid>.Failure(storeEncryptedResult.Errors);
            }

            return Result.Success(storeEncryptedResult.Value);
        }

        private async Task<IEnumerable<FileInfoViewModel>> CreateViewModel()
        {
            Guid userId = (Guid)userResolverService.GetUserGuid(User)!;

            List<FileInfoDto> fileList = await fileStorageService.GetFileListAsync(userId);

            return fileList.Select(f =>
                new FileInfoViewModel()
                {
                    Id = f.Id,
                    ContentType = f.ContentType,
                    FileName = f.FileName,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt.ToLocalTime().ToString()
                }
            );
        }

        // List current user's files
        public async Task<IActionResult> Index()
        {
            var viewModel = await CreateViewModel();

            return View(viewModel);
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

            // Validate all files before any are processed!
            // NOTE: For a general-purpose file manager that doesn't execute or read the stored files, 
            // extension / content-type checking is not really necessary (and both can be faked by user anyway).
            // So this is just for the sake of implementing it (required by task)
            foreach (IFormFile file in vm.Files)
            {
                Result validation = fileValidator.Validate(new FileUploadInfoDto()
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length
                });

                if (!validation.IsSuccess)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Validation error ({file.FileName}), no files were stored: {string.Join(" ", validation.Errors.Select(e => e.Message))}"
                    );
                    // TOOD: Maybe validate all files before returning, but it complicates things
                    return View(vm);
                }
            }

            foreach (IFormFile file in vm.Files)
            {
                var processResult = await ProcessUploadedFile(file);

                if (!processResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, $"Error: {string.Join(" ", processResult.Errors.Select(e => e.Message))}");
                    return View(vm);
                }
            }

            return RedirectToAction("Index");
        }

        // Download file by ID
        public async Task<IActionResult> Download([FromRoute] Guid id)
        {
            Guid userId = (Guid)userResolverService.GetUserGuid(User)!;
            // TODO: Reduce possible file content copies here

            var getFileResult = await fileStorageService.GetFileAsync(id, userId);
            if (!getFileResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, "File not found.");
                return View("Index", await CreateViewModel());
            }

            var decryptResult = await fileEncryptionService.DecryptAsync(
                getFileResult.Value!.File,
                getFileResult.Value!.Info.Id
            );
            if (!decryptResult.IsSuccess)
            {
                // TODO: Return correct result
                ModelState.AddModelError(string.Empty, $"File '{getFileResult.Value!.Info.FileName}' has been modified and can't be decrypted.");
                return View("Index", await CreateViewModel());
            }

            var fileInfo = getFileResult.Value!.Info;
            return File(decryptResult.Value!, fileInfo.ContentType, fileInfo.FileName);
        }

        // Delete file by id
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            Guid userId = (Guid)userResolverService.GetUserGuid(User)!;

            var deleteResult = await fileStorageService.DeleteFileAsync(id, userId);

            if (!deleteResult.IsSuccess)
            {
                // TODO: Return correct result
                ModelState.AddModelError(string.Empty, "File not found.");
                return View("Index", await CreateViewModel());
            }

            return RedirectToAction("Index");
        }
    }
}