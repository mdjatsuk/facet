using FacetApi.Models;
using FacetApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Services;

public class DocumentService : IDocumentService
{
    private readonly FacetDbContext _db;
    private readonly string _storageRoot;
    private readonly ILogger<DocumentService> _logger;

    private const long MAX_UPLOAD_BYTES = 10 * 1024 * 1024; // 10MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };

    public DocumentService(FacetDbContext db, IWebHostEnvironment env, ILogger<DocumentService> logger)
    {
        _db = db;
        _storageRoot = Path.Combine(env.ContentRootPath, "storage");
        _logger = logger;
        Directory.CreateDirectory(_storageRoot);
    }

    public IQueryable<Document> Query() => _db.Documents.AsNoTracking().OrderByDescending(d => d.UploadedAt);
    public async Task<Document?> GetAsync(Guid id) => await _db.Documents.FindAsync(id);

    public (Stream Stream, string ContentType, string FileName)? GetFile(Guid id)
    {
        var doc = _db.Documents.Find(id);
        if (doc is null) return null;
        var fullPath = Directory.GetFiles(_storageRoot, $"{id}_*").FirstOrDefault();
        if (fullPath is null) return null;
        // Open for read and allow other processes to read/write where possible to reduce file locks.
        try
        {
            var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return (fs, doc.ContentType, doc.FileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open file for id={Id} path={Path}", id, fullPath);
            throw;
        }
    }

    public async Task<UploadResult> UploadAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return new UploadResult(false, "Faili ei leitud või fail on tühi.", null);
        if (file.Length > MAX_UPLOAD_BYTES)
            return new UploadResult(false, "Fail on liiga suur. Lubatud kuni 10 MB.", null);

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return new UploadResult(false, "Pole toetatud failitüüp. Lubatud: PDF, JPG, PNG.", null);

        var contentType = !string.IsNullOrWhiteSpace(file.ContentType) ? file.ContentType : ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        if (!AllowedContentTypes.Contains(contentType))
            return new UploadResult(false, "Pole toetatud failitüüp. Lubatud: PDF, JPG, PNG.", null);

        var doc = new Document
        {
            FileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);

        var destPath = Path.Combine(_storageRoot, $"{doc.Id}_{doc.FileName}");
        using (var fs = new FileStream(destPath, FileMode.CreateNew))
            await file.CopyToAsync(fs);

        await _db.SaveChangesAsync();
        return new UploadResult(true, "Fail on edukalt üles laetud", doc);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return false;
        var path = Directory.GetFiles(_storageRoot, $"{id}_*").FirstOrDefault();
        if (path is not null && File.Exists(path))
        {
            // Try to delete with retries in case a different process still holds the file handle
            const int maxAttempts = 5;
            var attempt = 0;
            var delay = 100; // ms
            while (true)
            {
                try
                {
                    File.Delete(path);
                    _logger?.LogInformation("Deleted file path={Path} for id={Id}", path, id);
                    break;
                }
                catch (IOException ioEx)
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        _logger?.LogError(ioEx, "Failed to delete file after {Attempts} attempts path={Path} id={Id}", attempt, path, id);
                        // swallow and proceed to remove DB record to avoid leaving stale DB entries, but still notify
                        break;
                    }
                    _logger?.LogWarning(ioEx, "File in use, retrying delete attempt {Attempt} for path={Path}", attempt, path);
                    await Task.Delay(delay);
                    delay *= 2;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected error deleting file path={Path} id={Id}", path, id);
                    break;
                }
            }
        }
            
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }

}
