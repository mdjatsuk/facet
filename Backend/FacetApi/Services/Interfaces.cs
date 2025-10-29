using Microsoft.AspNetCore.Http;
using FacetApi.Models;

namespace FacetApi.Services;

public interface IDocumentService
{
    IQueryable<Document> Query();
    Task<Document?> GetAsync(Guid id);
    Task<UploadResult> UploadAsync(IFormFile file);
    (Stream Stream, string ContentType, string FileName)? GetFile(Guid id);
    Task<string?> GetPreviewAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);
    
    Task<(Stream Stream, string FileName)?> RedactPdfAsync(Guid id, List<string> valuesToHide);
    
    Task<Models.UploadResult> CreateRedactedCopyAsync(Guid id, List<string> valuesToHide);

}
