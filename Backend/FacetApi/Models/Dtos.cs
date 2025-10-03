using FacetApi.Models;

namespace FacetApi.Models;
public record UploadResult(bool Success, string Message, Document? Document);
