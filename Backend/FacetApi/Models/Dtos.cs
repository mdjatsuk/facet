using FacetApi.Models;

namespace FacetApi.Models;

public record SensitiveItem(string Type, string Value, int IndexStart, int IndexEnd);

public record UploadResult(bool Success, string Message, Document? Document, List<SensitiveItem>? Detected = null);
