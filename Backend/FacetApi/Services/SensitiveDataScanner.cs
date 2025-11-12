using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace FacetApi.Services;

public class SensitiveDataScanner : ISensitiveDataScanner
{
    private readonly ILogger<SensitiveDataScanner> _logger;

    private static readonly Regex EmailRe = new(
        pattern:
        @"(?<![A-Za-z0-9._%])" +                             
        @"[A-Za-z0-9](?:[A-Za-z0-9._%+-]*[A-Za-z0-9])?" +    
        @"@" +
        @"[A-Za-z0-9.-]+\.[A-Za-z]{2,24}" +                  
        @"(?=$|[^A-Za-z]|[A-Z])",                            
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    private static readonly Regex PhoneRe = new(
        pattern:
        @"(?<!\d)\+\d(?:[\s\.\-\(\)\u00A0\u202F\u200B]*\d){6,14}(?!\d)",
        options: RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

private static readonly Regex IbanRe = new(
    pattern: @"\b[A-Z]{2}\d{2}[A-Z0-9]{11,30}\b",
    options: RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
    matchTimeout: TimeSpan.FromMilliseconds(250));

    private static readonly Regex EeIdRe = new(
        pattern: @"(?<!\d)([1-6]\d{2}(?:0[1-9]|1[0-2])(?:0[1-9]|[12]\d|3[01])\d{4})(?!\d)",
        options: RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    public SensitiveDataScanner(ILogger<SensitiveDataScanner> logger) => _logger = logger;

    public List<Models.SensitiveItem> ScanText(string text)
    {
        var results = new List<Models.SensitiveItem>();
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("ScanText called with empty text");
            return results;
        }

        var norm = NormalizeForScan(text);

        var used = new bool[norm.Length];
        bool IsUsed(int s, int e) { for (int i = s; i < e && i < used.Length; i++) if (used[i]) return true; return false; }
        void MarkUsed(int s, int e) { for (int i = s; i < e && i < used.Length; i++) used[i] = true; }

        foreach (Match m in EmailRe.Matches(norm))
        {
            if (m.Length == 0 || IsUsed(m.Index, m.Index + m.Length)) continue;
            var value = m.Value;

            if (value.Contains("..") || value.StartsWith('.') || value.EndsWith('.')) continue;

            results.Add(new Models.SensitiveItem("email", value, m.Index, m.Index + m.Length));
            MarkUsed(m.Index, m.Index + m.Length);
        }

        foreach (Match m in IbanRe.Matches(norm))
        {
            if (m.Length == 0 || IsUsed(m.Index, m.Index + m.Length)) continue;

            var raw = m.Value;
            var compact = CompactToken(raw);             
            if (IsValidIban(compact))                     
            {
                results.Add(new Models.SensitiveItem("iban", raw, m.Index, m.Index + m.Length));
                MarkUsed(m.Index, m.Index + m.Length);
            }
            else
            {
                _logger.LogDebug("Rejected IBAN candidate (len={Len}): {Raw}", compact.Length, raw);
            }
        }

        foreach (Match m in PhoneRe.Matches(norm))
        {
            if (m.Length == 0 || IsUsed(m.Index, m.Index + m.Length)) continue;

            var raw = m.Value;
            if (Regex.IsMatch(raw, @"\b\d{4}[-/.]\d{1,2}[-/.]\d{1,2}\b"))
            {
                _logger.LogDebug("Rejected phone (date-like): {Raw}", raw);
                continue;
            }
            var digits = DigitsOnly(raw);
            if (digits.Length < 7 || digits.Length > 15)
            {
                _logger.LogDebug("Rejected phone (digits={Len}): {Raw}", digits.Length, raw);
                continue;
            }

            results.Add(new Models.SensitiveItem("phone", raw, m.Index, m.Index + m.Length));
            MarkUsed(m.Index, m.Index + m.Length);
        }

        foreach (Match m in EeIdRe.Matches(norm))
        {
            if (m.Length == 0 || IsUsed(m.Index, m.Index + m.Length)) continue;

            var code = m.Groups[1].Value;
            if (IsValidEstonianId(code))
            {
                results.Add(new Models.SensitiveItem("id", code, m.Index, m.Index + m.Length));
                MarkUsed(m.Index, m.Index + m.Length);
            }
            else
            {
                _logger.LogDebug("Rejected EE ID candidate: {Code}", code);
            }
        }

        _logger.LogInformation("Found {Count} sensitive items", results.Count);
        return results;
    }

    public List<Models.SensitiveItem> ScanPdfStream(Stream pdfStream)
    {
        _logger.LogInformation("Starting PDF scan, stream length: {Length}", pdfStream.Length);
        using var doc = PdfDocument.Open(pdfStream);
        _logger.LogInformation("PDF opened; pages: {Pages}", doc.NumberOfPages);

        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            var txt = page.Text;
            if (!string.IsNullOrWhiteSpace(txt))
            {
                _logger.LogDebug("Page {P} len {L}", page.Number, txt.Length);
                sb.AppendLine(txt);
            }
        }
        return ScanText(sb.ToString());
    }


    private static string NormalizeForScan(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var norm = s
            .Replace('\u00A0', ' ') 
            .Replace('\u202F', ' ') 
            .Replace('\u2007', ' ')
            .Replace('\u200B', ' ') 
            .Replace('\u00AD', '-'); 

        norm = Regex.Replace(norm, @"\.(com|ee|eu|net|org|io|ai)(?=[A-Z])", ".$1 ");

        norm = Regex.Replace(norm, @"(?<=\+\d{6,})(?=[A-Za-z])", " ");

        return norm;
    }

    private static string CompactToken(string s)
    => Regex.Replace(s, @"[\p{Z}\s\p{Cf}\-._]", string.Empty);


    private static string DigitsOnly(string s)
        => Regex.Replace(s, @"\D", string.Empty);

    private static readonly Dictionary<string, int> IbanLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AL"]=28,["AD"]=24,["AT"]=20,["AZ"]=28,["BA"]=20,["BE"]=16,["BG"]=22,["BH"]=22,["BR"]=29,
        ["CH"]=21,["CR"]=22,["CY"]=28,["CZ"]=24,["DE"]=22,["DK"]=18,["DO"]=28,["EE"]=20,["ES"]=24,
        ["FI"]=18,["FO"]=18,["FR"]=27,["GB"]=22,["GE"]=22,["GI"]=23,["GL"]=18,["GR"]=27,["GT"]=28,
        ["HR"]=21,["HU"]=28,["IE"]=22,["IL"]=23,["IQ"]=23,["IS"]=26,["IT"]=27,["JO"]=30,["KW"]=30,
        ["KZ"]=20,["LB"]=28,["LC"]=32,["LI"]=21,["LT"]=20,["LU"]=20,["LV"]=21,["MC"]=27,["MD"]=24,
        ["ME"]=22,["MK"]=19,["MR"]=27,["MT"]=31,["MU"]=30,["NL"]=18,["NO"]=15,["PK"]=24,["PL"]=28,
        ["PS"]=29,["PT"]=25,["QA"]=29,["RO"]=24,["RS"]=22,["SA"]=24,["SC"]=31,["SE"]=24,["SI"]=19,
        ["SK"]=24,["SM"]=27,["ST"]=25,["SV"]=28,["TL"]=23,["TN"]=24,["TR"]=26,["UA"]=29,["VG"]=24,["XK"]=20
    };

    private static bool IsValidIban(string ibanRaw)
    {
        if (string.IsNullOrWhiteSpace(ibanRaw)) return false;

        var compact = Regex.Replace(ibanRaw, @"[\p{Z}\s\p{Cf}\-._]", string.Empty);

        var iban = compact.ToUpperInvariant();

        if (iban.Length < 15 || iban.Length > 34) return false;
        if (!Regex.IsMatch(iban, "^[A-Z0-9]+$")) return false;

        var cc = iban[..2];
        if (!IbanLengths.TryGetValue(cc, out var expected) || iban.Length != expected) return false;

        var rearranged = iban[4..] + iban[..4];
        int rem = 0;
        foreach (char ch in rearranged)
        {
            if (char.IsLetter(ch))
            {
                int val = ch - 'A' + 10; 
                foreach (char d in val.ToString())
                {
                    rem = (rem * 10 + (d - '0')) % 97;
                }
            }
            else
            {
                rem = (rem * 10 + (ch - '0')) % 97;
            }
        }
        return rem == 1;
    }

    private static bool IsValidEstonianId(string code)
    {
        if (!Regex.IsMatch(code, @"^\d{11}$")) return false;

        int[] d = code.Select(c => c - '0').ToArray();
        int g = d[0];
        if (g < 1 || g > 6) return false;

        int yy = d[1] * 10 + d[2];
        int mm = d[3] * 10 + d[4];
        int dd = d[5] * 10 + d[6];
        int century = g <= 2 ? 1800 : g <= 4 ? 1900 : 2000;
        int year = century + yy;

        if (!IsValidDate(year, mm, dd)) return false;

        int[] w1 = { 1,2,3,4,5,6,7,8,9,1 };
        int[] w2 = { 3,4,5,6,7,8,9,1,2,3 };

        int sum = 0; for (int i = 0; i < 10; i++) sum += d[i] * w1[i];
        int c = sum % 11;
        if (c == 10)
        {
            sum = 0; for (int i = 0; i < 10; i++) sum += d[i] * w2[i];
            c = sum % 11;
            if (c == 10) c = 0;
        }
        return c == d[10];
    }

    private static bool IsValidDate(int y, int m, int d)
    {
        if (m < 1 || m > 12) return false;
        try { var dt = new DateTime(y, m, d); return dt.Year == y && dt.Month == m && dt.Day == d; }
        catch { return false; }
    }
}