namespace FacetApi.Services;

public interface ISensitiveDataScanner
{
    List<Models.SensitiveItem> ScanText(string text);

    List<Models.SensitiveItem> ScanPdfStream(Stream pdfStream);
}