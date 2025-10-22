using FacetApi.Models;
using FacetApi.Data;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore;
using PdfSharpCore.Drawing.Layout;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace FacetApi.Services;

public class DocumentService : IDocumentService
{
    private readonly FacetDbContext _db;
    private readonly string _storageRoot;
    private readonly ILogger<DocumentService> _logger;
    
    private readonly ISensitiveDataScanner _scanner;

    private const long MAX_UPLOAD_BYTES = 10 * 1024 * 1024; // 10MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };

    public DocumentService(FacetDbContext db, IWebHostEnvironment env, ILogger<DocumentService> logger, ISensitiveDataScanner scanner)
    {
        _db = db;
        _storageRoot = Path.Combine(env.ContentRootPath, "storage");
        _logger = logger;
        _scanner = scanner;
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

        List<Models.SensitiveItem>? detected = null;
        try
        {
            if (contentType == "application/pdf")
            {
                // reopen file for reading
                using var fs = new FileStream(destPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                detected = _scanner.ScanPdfStream(fs);
            }
            else if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                // OCR is not included by default. If you add an OCR library (Tesseract), extract text here and call _scanner.ScanText(text)
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Sensitive data scanning failed for file {File}", destPath);
        }

        return new UploadResult(true, "Fail on edukalt üles laetud", doc, detected);
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

 public async Task<(Stream Stream, string FileName)?> RedactPdfAsync(Guid id, List<string> valuesToHide)
{
    var doc = await _db.Documents.FindAsync(id);
    if (doc is null) return null;
    if (!string.Equals(doc.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return null;

    var fullPath = Directory.GetFiles(_storageRoot, $"{id}_*").FirstOrDefault();
    if (fullPath is null) return null;

    var tokens = valuesToHide?
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s.Trim())
        .Where(s => !s.Contains("*"))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();

    if (tokens.Length == 0) return null;

    try
    {
        using var input = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(input);

        var output = new PdfSharpCore.Pdf.PdfDocument();
        var font = new XFont("Consolas", 10, XFontStyle.Regular); // Моноширинный для наглядности

        for (int i = 0; i < pdf.NumberOfPages; i++)
        {
            var page = pdf.GetPage(i + 1);
            var text = page.Text ?? string.Empty;

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;
                var masked = MaskValue("auto", token);
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    System.Text.RegularExpressions.Regex.Escape(token),
                    masked,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }

            text = text.Replace("\r", "");
            text = text.Replace("\t", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, " {2,}", " ");

            var newPage = output.AddPage();
            var gfx = XGraphics.FromPdfPage(newPage);
            var tf = new XTextFormatter(gfx);
            tf.Alignment = XParagraphAlignment.Left;
            var rect = new XRect(40, 40, newPage.Width - 80, newPage.Height - 80);

            tf.DrawString(text, font, XBrushes.Black, rect);
            gfx.Dispose();
        }

        var ms = new MemoryStream();
        output.Save(ms, false);
        ms.Position = 0;
    var outName = Path.GetFileNameWithoutExtension(doc.FileName) + "-redacted.pdf";
        return (ms, outName);
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Text redaction rewrite failed for id={Id}", id);
        return null;
    }
}


    public async Task<Models.UploadResult> CreateRedactedCopyAsync(Guid id, List<string> valuesToHide)
    {
        // Use overlay/vector redaction only (Ghostscript/rasterization removed by project decision)
        (Stream Stream, string FileName)? red = null;
        try
        {
            red = await RedactPdfAsync(id, valuesToHide);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Overlay redaction failed for id={Id}", id);
            return new Models.UploadResult(false, "Redaction failed", null, null);
        }
        if (red is null) return new Models.UploadResult(false, "Redaction failed or not a PDF", null, null);

        var (stream, fileName) = red.Value;
        // Persist new document record
        var newDoc = new Models.Document
        {
            FileName = fileName,
            ContentType = "application/pdf",
            SizeBytes = stream.Length,
            UploadedAt = DateTime.UtcNow
        };

        _db.Documents.Add(newDoc);

        var destPath = Path.Combine(_storageRoot, $"{newDoc.Id}_{newDoc.FileName}");
        // write stream to file
        using (var fs = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write))
        {
            stream.Position = 0;
            await stream.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        await _db.SaveChangesAsync();

        // Re-scan the new PDF to ensure no sensitive items remain. If scanner still finds items, replace with full-page blackout PDF as a safe fallback.
        List<Models.SensitiveItem>? detected = null;
        try
        {
            using var fsr = new FileStream(destPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            detected = _scanner.ScanPdfStream(fsr);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Scanning redacted copy failed for file {File}", destPath);
        }

        if (detected != null && detected.Count > 0)
        {
            // Don't automatically replace the user's file with a full blackout — keep the redacted copy
            // (which may still contain detectable items when rasterization wasn't available) and return
            // the detected items so the client can present the choice to the user.
            _logger?.LogWarning("Redacted copy still contains {Count} sensitive items; leaving redacted copy in place for file {File}", detected.Count, destPath);
        }

        return new Models.UploadResult(true, "Redacted copy created", newDoc, detected);
    }

    // Ghostscript rasterization removed — project uses overlay/vector redaction only.

    private string MaskValue(string kind, string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        // crude heuristics
        // EMAIL: always show domain, replace local-part with five stars: *****@domain
        if (value.Contains("@"))
        {
            var parts = value.Split('@');
            var domain = parts.Length > 1 ? parts[1] : "";
            return "*****" + (domain.Length > 0 ? "@" + domain : "");
        }

        // PHONE: special-case +372 -> +372****, other +countries -> keep country code then 4 stars
        if (value.StartsWith("+"))
        {
            var digits = System.Text.RegularExpressions.Regex.Replace(value, "\\D", "");
            if (value.StartsWith("+372"))
            {
                return "+372****";
            }
            // try to keep country code (up to 4 chars after +) then 4 stars
            var m = System.Text.RegularExpressions.Regex.Match(value, "^\\+(\\d{1,4})");
            if (m.Success)
            {
                var cc = m.Groups[1].Value;
                return "+" + cc + "****";
            }
            return new string('*', Math.Min(6, value.Length));
        }

        // IBAN: keep first 2 (country code) and last 4, mask middle with stars
        if (System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z]{2}[A-Za-z0-9]{6,}$"))
        {
            var len = value.Length;
            if (len <= 6) return new string('*', len);
            var first = value.Substring(0, 2);
            var last = value.Substring(Math.Max(0, len - 4));
            var middleStars = new string('*', Math.Max(4, len - first.Length - last.Length));
            return first + middleStars + last;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(value, "^\\d+$"))
        {
            return new string('*', value.Length);
        }

        return new string('*', Math.Min(10, value.Length));
    }

}
