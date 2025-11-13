using System.ComponentModel.DataAnnotations;

namespace FacetApi.Models;

public class Document
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(512)]
    public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(128)]
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsTemporary { get; set; } = true;
    public int? OwnerId { get; set; }
}
