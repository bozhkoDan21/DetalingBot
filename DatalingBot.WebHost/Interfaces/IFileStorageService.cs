using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
}
