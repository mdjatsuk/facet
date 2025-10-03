using FacetApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _svc;
    public DocumentsController(IDocumentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _svc.Query().ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var doc = await _svc.GetAsync(id);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        var result = await _svc.UploadAsync(file);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpGet("{id:guid}/file")]
    public IActionResult File(Guid id)
    {
        var res = _svc.GetFile(id);
        if (res is null) return NotFound();

        Response.Headers.ContentDisposition = $"inline; filename=\"{res.Value.FileName}\"";
        return new FileStreamResult(res.Value.Stream, res.Value.ContentType);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound("Fail ei leitud.");
        return NoContent(); 
    }

}
