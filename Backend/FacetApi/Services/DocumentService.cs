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
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using DocModel = FacetApi.Models.Document;

namespace FacetApi.Services;

public class DocumentService : IDocumentService
{
    private readonly FacetDbContext _db;
    private readonly string _storageRoot;
    private readonly ILogger<DocumentService> _logger;
    
    private readonly ISensitiveDataScanner _scanner;

    private const long MAX_UPLOAD_BYTES = 10 * 1024 * 1024; 
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx", ".txt" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { 
        "application/msword", 
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
        "text/plain" 
    };

    public DocumentService(FacetDbContext db, IWebHostEnvironment env, ILogger<DocumentService> logger, ISensitiveDataScanner scanner)
    {
        _db = db;
        _storageRoot = Path.Combine(env.ContentRootPath, "storage");
        _logger = logger;
        _scanner = scanner;
        Directory.CreateDirectory(_storageRoot);
    }

    public IQueryable<DocModel> Query() => _db.Documents.AsNoTracking().OrderByDescending(d => d.UploadedAt);
    public async Task<DocModel?> GetAsync(Guid id) => await _db.Documents.FindAsync(id);

    public (Stream Stream, string ContentType, string FileName)? GetFile(Guid id)
    {
        var doc = _db.Documents.Find(id);
        if (doc is null) return null;
        var fullPath = Directory.GetFiles(_storageRoot, $"{id}_*").FirstOrDefault();
        if (fullPath is null) return null;
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

    public async Task<string?> GetPreviewAsync(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return null;
        
        var fullPath = Directory.GetFiles(_storageRoot, $"{id}_*").FirstOrDefault();
        if (fullPath is null) return null;

        try
        {
            // Handle .txt files
            if (doc.ContentType == "text/plain")
            {
                var text = await File.ReadAllTextAsync(fullPath);
                return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ 
            font-family: 'Courier New', monospace; 
            white-space: pre-wrap; 
            padding: 20px; 
            background: white; 
            color: black;
            margin: 0;
        }}
    </style>
</head>
<body>{System.Net.WebUtility.HtmlEncode(text)}</body>
</html>";
            }
            
            // Handle .docx files
            if (doc.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                return ConvertDocxToHtmlAsync(fullPath);
            }
            
            // Handle .doc files (legacy format)
            if (doc.ContentType == "application/msword")
            {
                // .doc format requires different handling - for now return a message
                return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; }}
        .message {{ background: #f0f0f0; padding: 20px; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='message'>
        <h3>Legacy .doc format</h3>
        <p>Please download the file to view it. Convert to .docx format for inline preview.</p>
        <a href='/api/documents/{id}/file' download>Download File</a>
    </div>
</body>
</html>";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate preview for id={Id}", id);
            return null;
        }
    }

    private string ConvertDocxToHtmlAsync(string docxPath)
    {
        var html = new StringBuilder();
        html.Append(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { 
            font-family: 'Calibri', 'Arial', sans-serif; 
            padding: 40px; 
            background: white; 
            color: black;
            line-height: 1.6;
            max-width: 800px;
            margin: 0 auto;
        }
        p { margin: 0 0 10px 0; }
        h1 { font-size: 2em; margin: 0.67em 0; }
        h2 { font-size: 1.5em; margin: 0.75em 0; }
        h3 { font-size: 1.17em; margin: 0.83em 0; }
        .bold { font-weight: bold; }
        .italic { font-style: italic; }
        .underline { text-decoration: underline; }
        table { border-collapse: collapse; margin: 10px 0; }
        td, th { border: 1px solid #ddd; padding: 8px; }
    </style>
</head>
<body>");

        using (var doc = WordprocessingDocument.Open(docxPath, false))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                foreach (var element in body.Elements())
                {
                    if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
                    {
                        var paraHtml = ConvertParagraphToHtml(para);
                        html.Append(paraHtml);
                    }
                    else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
                    {
                        html.Append("<table>");
                        foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                        {
                            html.Append("<tr>");
                            foreach (var cell in row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>())
                            {
                                html.Append("<td>");
                                foreach (var cellPara in cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                                {
                                    html.Append(ConvertParagraphToHtml(cellPara));
                                }
                                html.Append("</td>");
                            }
                            html.Append("</tr>");
                        }
                        html.Append("</table>");
                    }
                }
            }
        }

        html.Append("</body></html>");
        return html.ToString();
    }

    private string ExtractTextFromDocxAsync(string docxPath)
    {
        var text = new StringBuilder();
        
        using (var doc = WordprocessingDocument.Open(docxPath, false))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                text.Append(body.InnerText);
            }
        }
        
        return text.ToString();
    }

    private async Task<(Stream Stream, string FileName)> RedactDocxAsync(string docxPath, string[] tokens, string originalFileName)
    {
        // Create a copy in memory
        var ms = new MemoryStream();
        using (var sourceStream = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            await sourceStream.CopyToAsync(ms);
        }
        ms.Position = 0;

        // Open and modify the copy
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                foreach (var text in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                {
                    if (string.IsNullOrWhiteSpace(text.Text)) continue;
                    
                    var modifiedText = text.Text;
                    foreach (var token in tokens)
                    {
                        if (string.IsNullOrWhiteSpace(token)) continue;
                        var masked = MaskValue("auto", token);
                        modifiedText = System.Text.RegularExpressions.Regex.Replace(
                            modifiedText,
                            System.Text.RegularExpressions.Regex.Escape(token),
                            masked,
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        );
                    }
                    
                    if (modifiedText != text.Text)
                    {
                        text.Text = modifiedText;
                    }
                }
            }
            
            doc.MainDocumentPart?.Document?.Save();
        }

        ms.Position = 0;
        var outName = Path.GetFileNameWithoutExtension(originalFileName) + "-redacted.docx";
        return (ms, outName);
    }

    private string ConvertParagraphToHtml(DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
    {
        var sb = new StringBuilder();
        var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        
        var tag = style switch
        {
            "Heading1" => "h1",
            "Heading2" => "h2",
            "Heading3" => "h3",
            _ => "p"
        };

        sb.Append($"<{tag}>");

        foreach (var run in para.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
        {
            var text = run.InnerText;
            var isBold = run.RunProperties?.Bold != null;
            var isItalic = run.RunProperties?.Italic != null;
            var isUnderline = run.RunProperties?.Underline != null;
            
            var classes = new List<string>();
            if (isBold) classes.Add("bold");
            if (isItalic) classes.Add("italic");
            if (isUnderline) classes.Add("underline");

            if (classes.Any())
            {
                sb.Append($"<span class='{string.Join(" ", classes)}'>");
                sb.Append(System.Net.WebUtility.HtmlEncode(text));
                sb.Append("</span>");
            }
            else
            {
                sb.Append(System.Net.WebUtility.HtmlEncode(text));
            }
        }

        sb.Append($"</{tag}>");
        return sb.ToString();
    }

    public async Task<UploadResult> UploadAsync(IFormFile file, int? ownerId)
    {
        if (file is null || file.Length == 0)
            return new UploadResult(false, "Faili ei leitud või fail on tühi.", null);
        if (file.Length > MAX_UPLOAD_BYTES)
            return new UploadResult(false, "Fail on liiga suur. Lubatud kuni 10 MB.", null);

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return new UploadResult(false, "Pole toetatud failitüüp. Lubatud: DOC, DOCX, TXT.", null);

        var contentType = !string.IsNullOrWhiteSpace(file.ContentType) ? file.ContentType : ext switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
        if (!AllowedContentTypes.Contains(contentType))
            return new UploadResult(false, "Pole toetatud failitüüp. Lubatud: DOC, DOCX, TXT.", null);

        // Save original file (no auto-redaction; redaction happens only on Apply Changes)
        var doc = new DocModel
        {
            FileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            IsTemporary = true,
            OwnerId = ownerId
        };
        _db.Documents.Add(doc);

        var destPath = Path.Combine(_storageRoot, $"{doc.Id}_{doc.FileName}");
        using (var fs = new FileStream(destPath, FileMode.CreateNew))
            await file.CopyToAsync(fs);

        await _db.SaveChangesAsync();

        List<Models.SensitiveItem>? detected = null;
        try
        {
            if (contentType == "text/plain")
            {
                var text = await File.ReadAllTextAsync(destPath);
                detected = _scanner.ScanText(text);
            }
            else if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                var text = ExtractTextFromDocxAsync(destPath);
                detected = _scanner.ScanText(text);
            }
            else if (contentType == "application/msword")
            {
                _logger?.LogInformation("Skipping scanning for legacy .doc format: {File}", destPath);
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
            const int maxAttempts = 5;
            var attempt = 0;
            var delay = 100; 
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
        // Handle text files
        if (doc.ContentType == "text/plain")
        {
            var text = await File.ReadAllTextAsync(fullPath);
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
            
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms, System.Text.Encoding.UTF8);
            await writer.WriteAsync(text);
            await writer.FlushAsync();
            ms.Position = 0;
            var outName = Path.GetFileNameWithoutExtension(doc.FileName) + "-redacted.txt";
            return (ms, outName);
        }
        
        // Handle .docx files
        if (doc.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return await RedactDocxAsync(fullPath, tokens, doc.FileName);
        }
        
        // Legacy .doc files not supported for redaction
        if (doc.ContentType == "application/msword")
        {
            return null;
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Redaction failed for id={Id}", id);
        return null;
    }
}


    public async Task<Models.UploadResult> CreateRedactedCopyAsync(Guid id, List<string> valuesToHide)
    {
        (Stream Stream, string FileName)? red = null;
        try
        {
            red = await RedactPdfAsync(id, valuesToHide);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Redaction failed for id={Id}", id);
            return new Models.UploadResult(false, "Redaction failed", null, null);
        }
        if (red is null) return new Models.UploadResult(false, "Redaction failed or unsupported format", null, null);

        var (stream, fileName) = red.Value;
        
        // Determine content type from file extension
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
        
        // Preserve owner of the original document so the redacted copy is visible to the same user
        var originalOwnerId = (await _db.Documents.FindAsync(id))?.OwnerId;

        var newDoc = new DocModel
        {
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = stream.Length,
            UploadedAt = DateTime.UtcNow,
            IsTemporary = false
            , OwnerId = originalOwnerId
        };

        _db.Documents.Add(newDoc);

        var destPath = Path.Combine(_storageRoot, $"{newDoc.Id}_{newDoc.FileName}");
        using (var fs = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write))
        {
            stream.Position = 0;
            await stream.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        await _db.SaveChangesAsync();

        List<Models.SensitiveItem>? detected = null;
        try
        {
            if (contentType == "text/plain")
            {
                var text = await File.ReadAllTextAsync(destPath);
                detected = _scanner.ScanText(text);
            }
            else if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                var text = ExtractTextFromDocxAsync(destPath);
                detected = _scanner.ScanText(text);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Scanning redacted copy failed for file {File}", destPath);
        }

        if (detected != null && detected.Count > 0)
        {
            _logger?.LogWarning("Redacted copy still contains {Count} sensitive items; leaving redacted copy in place for file {File}", detected.Count, destPath);
        }

        // Delete the original document (security: do not store original uploads)
        try
        {
            var originalDoc = await _db.Documents.FindAsync(id);
            if (originalDoc != null)
            {
                // Delete physical file from storage
                var originalPath = Path.Combine(_storageRoot, $"{originalDoc.Id}_{originalDoc.FileName}");
                if (File.Exists(originalPath))
                {
                    File.Delete(originalPath);
                    _logger?.LogInformation("Deleted original document file: {Path}", originalPath);
                }

                // Delete from database
                _db.Documents.Remove(originalDoc);
                await _db.SaveChangesAsync();
                _logger?.LogInformation("Deleted original document record from database: Id={Id}", id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete original document after redaction: Id={Id}", id);
            // Don't fail the redaction operation if deletion fails; redacted copy is safely stored
        }

        return new Models.UploadResult(true, "Redacted copy created", newDoc, detected);
    }

    private string MaskValue(string kind, string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Contains("@"))
        {
            var parts = value.Split('@');
            var domain = parts.Length > 1 ? parts[1] : "";
            return "*****" + (domain.Length > 0 ? "@" + domain : "");
        }

        if (value.StartsWith("+"))
        {
            var digits = System.Text.RegularExpressions.Regex.Replace(value, "\\D", "");
            if (value.StartsWith("+372"))
            {
                return "+372****";
            }
            var m = System.Text.RegularExpressions.Regex.Match(value, "^\\+(\\d{1,4})");
            if (m.Success)
            {
                var cc = m.Groups[1].Value;
                return "+" + cc + "****";
            }
            return new string('*', Math.Min(6, value.Length));
        }

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
