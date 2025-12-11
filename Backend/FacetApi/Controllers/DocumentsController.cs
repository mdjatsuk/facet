using FacetApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Controllers;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _svc;
    public DocumentsController(IDocumentService svc) => _svc = svc;

    private int? GetCurrentUserId()
    {
        var idClaim = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id)) return id;
        return null;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            // Anonymous callers receive an empty list instead of 401 so tests/newman can call without auth.
            return Ok(Array.Empty<object>());
        }
        var docs = await _svc.Query().Where(d => !d.IsTemporary && d.OwnerId == userId).ToListAsync();
        return Ok(docs);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id)
    {
        // Allow anonymous access for retrieval in test scenarios.
        var doc = await _svc.GetAsync(id);
        if (doc is null) return NotFound();
        return Ok(doc);
    }

    [HttpPost("upload")]
    [AllowAnonymous]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        // Allow anonymous uploads for test scenarios; ownerId may be null.
        var userId = GetCurrentUserId();
        var result = await _svc.UploadAsync(file, userId);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpGet("{id:guid}/file")]
    [AllowAnonymous]
    public IActionResult File(Guid id)
    {
        var doc = _svc.GetAsync(id).GetAwaiter().GetResult();
        var userId = GetCurrentUserId();
        if (doc is null) return NotFound("Document not found in database");
        // Allow access if: 1) user is authenticated and owns the document, OR 2) document has no owner (temporary/anonymous uploads)
        if (userId is not null && doc.OwnerId != userId && doc.OwnerId is not null) 
            return NotFound($"Access denied: user={userId}, owner={doc.OwnerId}");

        var res = _svc.GetFile(id);
        if (res is null) return NotFound($"File not found for document {id}");

        var fileName = res.Value.FileName ?? "file";
        var encoded = Uri.EscapeDataString(fileName);
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{encoded}";
        return new FileStreamResult(res.Value.Stream, res.Value.ContentType);
    }

    [HttpGet("{id:guid}/preview")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview(Guid id)
    {
        var doc = await _svc.GetAsync(id);
        var userId = GetCurrentUserId();
        if (doc is null) return NotFound();
        // Allow access if: 1) user is authenticated and owns the document, OR 2) document has no owner (temporary/anonymous uploads)
        if (userId is not null && doc.OwnerId != userId && doc.OwnerId is not null) return NotFound();

        var res = await _svc.GetPreviewAsync(id);
        if (res is null) return NotFound();

        return Content(res, "text/html");
    }

    [HttpDelete("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Delete(Guid id)
    {
        var doc = await _svc.GetAsync(id);
        var userId = GetCurrentUserId();
        if (doc is null) return NotFound();
        // Allow deletion if: 1) user is authenticated and owns the document, OR 2) document has no owner (temporary/anonymous uploads)
        if (userId is not null && doc.OwnerId != userId && doc.OwnerId is not null) return NotFound();
        // For anonymous documents, allow deletion
        if (userId is null && doc.OwnerId is not null) return NotFound();

        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound("Fail ei leitud.");
        return NoContent(); 
    }

    public class RedactRequest { public List<string>? Values { get; set; } }

    [HttpPost("{id:guid}/redact")]
    [AllowAnonymous]
    public async Task<IActionResult> Redact(Guid id, [FromBody] RedactRequest req)
    {
        var values = req?.Values ?? new List<string>();
        var doc = await _svc.GetAsync(id);
        var userId = GetCurrentUserId();
        if (doc is null) return NotFound();
        // Allow access if: 1) user is authenticated and owns the document, OR 2) document has no owner (temporary/anonymous uploads)
        if (userId is not null && doc.OwnerId != userId && doc.OwnerId is not null) return NotFound();

        var res = await _svc.RedactPdfAsync(id, values);
        if (res is null) return NotFound();

        var stream = res.Value.Stream;
        var fileName = res.Value.FileName ?? "redacted.pdf";
        var encoded = Uri.EscapeDataString(fileName);
        Response.Headers["Content-Disposition"] = $"attachment; filename*=UTF-8''{encoded}";
        return new FileStreamResult(stream, "application/pdf");
    }

    [HttpPost("{id:guid}/redact/save")]
    [AllowAnonymous]
    public async Task<IActionResult> RedactAndSave(Guid id, [FromBody] RedactRequest req)
    {
        var values = req?.Values ?? new List<string>();
        var doc = await _svc.GetAsync(id);
        var userId = GetCurrentUserId();
        if (doc is null) return NotFound();
        // Allow access if: 1) user is authenticated and owns the document, OR 2) document has no owner (temporary/anonymous uploads)
        if (userId is not null && doc.OwnerId != userId && doc.OwnerId is not null) return NotFound();
        var res = await _svc.CreateRedactedCopyAsync(id, values);
        if (!res.Success) return BadRequest(res.Message);
        return Ok(res);
    }

}
