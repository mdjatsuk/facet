using Microsoft.AspNetCore.Http;
using FacetApi.Models;

namespace FacetApi.Services;

public interface IDocumentService
{
    IQueryable<Document> Query();
    Task<Document?> GetAsync(Guid id);
    Task<UploadResult> UploadAsync(IFormFile file);
    (Stream Stream, string ContentType, string FileName)? GetFile(Guid id);

    Task<bool> DeleteAsync(Guid id);

}
