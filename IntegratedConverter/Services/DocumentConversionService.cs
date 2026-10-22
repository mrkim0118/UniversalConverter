using System.IO;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using HtmlAgilityPack;
using NPOI.HSSF.UserModel;
using NPOI.XWPF.UserModel;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace UniversalConverter.Services;

public static class DocumentConversionService
{
    // ── 진입점 ─────────────────────────────────────────────────
    public static void Convert(string inputPath, string outputPath, string fromFmt, string toFmt)
    {
        var from = fromFmt.ToUpper();
        var to   = toFmt.ToUpper();

        // 바이너리 출력 포맷 (직접 파일 쓰기)
        switch ((from, to))
        {
            case ("TXT",  "PDF"):
            case ("HTML", "PDF"):
            case ("DOCX", "PDF"):
            case ("RTF",  "PDF"):
                TextToPdf(ExtractPlainText(inputPath, from), outputPath);
                return;

            case ("TXT",  "DOCX"):
            case ("HTML", "DOCX"):
                TextToDocx(ExtractPlainText(inputPath, from), outputPath);
                return;

            case ("CSV", "XLS"):
                CsvToXls(inputPath, outputPath);
                return;
        }

        // 텍스트 출력 포맷
        var content = (from, to) switch
        {
            ("PDF",  "TXT")  => PdfToTxt(inputPath),
            ("PDF",  "HTML") => WrapHtml("PDF 변환 결과", Pre(HtmlEnc(PdfToTxt(inputPath)))),

            ("DOCX", "TXT")  => DocxToTxt(inputPath),
            ("DOCX", "HTML") => DocxToHtml(inputPath),

            ("HTML", "TXT")  => HtmlToTxt(inputPath),

            ("TXT",  "HTML") => TxtToHtml(inputPath),

            ("RTF",  "TXT")  => RtfToTxt(inputPath),
            ("RTF",  "HTML") => WrapHtml("RTF 변환 결과", Pre(HtmlEnc(RtfToTxt(inputPath)))),

            ("CSV",  "JSON") => CsvToJson(inputPath),
            ("CSV",  "XML")  => CsvToXml(inputPath),
            ("CSV",  "HTML") => CsvToHtml(inputPath),
            ("CSV",  "TXT")  => CsvToTxt(inputPath),

            ("JSON", "XML")  => JsonToXml(inputPath),
            ("JSON", "CSV")  => JsonToCsv(inputPath),
            ("JSON", "HTML") => JsonToHtml(inputPath),
            ("JSON", "TXT")  => JsonToTxt(inputPath),

            ("XML",  "JSON") => XmlToJson(inputPath),
            ("XML",  "CSV")  => XmlToCsv(inputPath),
            ("XML",  "HTML") => XmlToHtml(inputPath),
            ("XML",  "TXT")  => XmlToTxt(inputPath),

            ("XLS",  "CSV")  => XlsToCsv(inputPath),
            ("XLS",  "JSON") => XlsToJson(inputPath),
            ("XLS",  "HTML") => XlsToHtml(inputPath),
            ("XLS",  "TXT")  => XlsToTxt(inputPath),

            _ => throw new NotSupportedException($"지원하지 않는 변환: {from} → {to}")
        };

        File.WriteAllText(outputPath, content, Encoding.UTF8);
    }

    // ── 텍스트 추출 공통 ────────────────────────────────────────
    private static string ExtractPlainText(string path, string fmt) => fmt switch
    {
        "TXT"  => File.ReadAllText(path, Encoding.UTF8),
        "HTML" => HtmlToTxt(path),
        "DOCX" => DocxToTxt(path),
        "RTF"  => RtfToTxt(path),
        _      => File.ReadAllText(path, Encoding.UTF8)
    };

    // ── PDF ────────────────────────────────────────────────────
    private static string PdfToTxt(string path)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static void TextToPdf(string text, string outputPath)
    {
        var pdfDoc = new PdfDocument();
        const double margin = 50;
        const double lineH  = 17;
        var font = new XFont("Malgun Gothic", 11);

        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = margin;

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (page == null || y + lineH > 841 - margin)   // A4 height ~841 pt
            {
                page = pdfDoc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx  = XGraphics.FromPdfPage(page);
                y    = margin;
            }
            if (!string.IsNullOrEmpty(raw))
                gfx!.DrawString(raw, font, XBrushes.Black,
                    new XRect(margin, y, page!.Width - margin * 2, lineH),
                    XStringFormats.TopLeft);
            y += lineH;
        }

        if (pdfDoc.PageCount == 0) pdfDoc.AddPage();
        pdfDoc.Save(outputPath);
    }

    // ── DOCX (XWPFDocument) ────────────────────────────────────
    private static string DocxToTxt(string path)
    {
        using var stream = File.OpenRead(path);
        var doc = new XWPFDocument(stream);
        var sb = new StringBuilder();
        foreach (var p in doc.Paragraphs)
            if (!string.IsNullOrWhiteSpace(p.Text))
                sb.AppendLine(p.Text);
        return sb.ToString();
    }

    private static string DocxToHtml(string path)
    {
        using var stream = File.OpenRead(path);
        var doc = new XWPFDocument(stream);
        var sb = new StringBuilder();
        foreach (var p in doc.Paragraphs)
        {
            if (string.IsNullOrWhiteSpace(p.Text)) continue;
            var tag = p.Style?.StartsWith("Heading") == true ? "h2" : "p";
            sb.AppendLine($"<{tag}>{HtmlEnc(p.Text)}</{tag}>");
        }
        return WrapHtml(Path.GetFileNameWithoutExtension(path), sb.ToString());
    }

    private static void TextToDocx(string text, string outputPath)
    {
        var doc = new XWPFDocument();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var para = doc.CreateParagraph();
            para.CreateRun().SetText(line);
        }
        using var stream = File.Create(outputPath);
        doc.Write(stream);
    }

    // ── HTML ───────────────────────────────────────────────────
    private static string HtmlToTxt(string path)
    {
        var html = File.ReadAllText(path, Encoding.UTF8);
        var hd   = new HtmlDocument();
        hd.LoadHtml(html);

        var sb = new StringBuilder();
        foreach (var node in hd.DocumentNode.DescendantsAndSelf())
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var t = node.InnerText.Trim();
                if (!string.IsNullOrEmpty(t)) sb.AppendLine(t);
            }
            else if (node.Name is "br" or "p" or "div" or "li")
                sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static string TxtToHtml(string path)
    {
        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            sb.AppendLine($"<p>{HtmlEnc(line)}</p>");
        return WrapHtml(Path.GetFileNameWithoutExtension(path), sb.ToString());
    }

    // ── RTF ────────────────────────────────────────────────────
    private static string RtfToTxt(string path)
    {
        // WPF RichTextBox를 사용한 RTF → TXT
        string result = "";
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var rtb = new System.Windows.Controls.RichTextBox();
            using var fs = File.OpenRead(path);
            var tr = new System.Windows.Documents.TextRange(
                rtb.Document.ContentStart, rtb.Document.ContentEnd);
            tr.Load(fs, System.Windows.DataFormats.Rtf);
            result = new System.Windows.Documents.TextRange(
                rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;
        });
        return result;
    }

    // ── CSV ────────────────────────────────────────────────────
    private static List<List<string>> ReadCsv(string path)
    {
        var rows = new List<List<string>>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            rows.Add(SplitCsvLine(line));
        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb     = new StringBuilder();
        var inQ    = false;
        foreach (var c in line)
        {
            if (c == '"') inQ = !inQ;
            else if (c == ',' && !inQ) { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields;
    }

    private static string CsvToJson(string path)
    {
        var rows = ReadCsv(path);
        if (rows.Count == 0) return "[]";
        var headers = rows[0];
        var records = rows.Skip(1)
            .Where(r => r.Any(f => !string.IsNullOrWhiteSpace(f)))
            .Select(r => headers
                .Select((h, i) => (h, v: i < r.Count ? r[i] : ""))
                .ToDictionary(t => t.h, t => (object)t.v));
        return JsonSerializer.Serialize(records,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static string CsvToXml(string path)
    {
        var rows = ReadCsv(path);
        if (rows.Count == 0) return "<records/>";
        var headers = rows[0];
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<records>\n");
        foreach (var row in rows.Skip(1).Where(r => r.Any(f => !string.IsNullOrWhiteSpace(f))))
        {
            sb.AppendLine("  <record>");
            for (var i = 0; i < headers.Count; i++)
            {
                var tag = SanitizeTag(headers[i]);
                var val = i < row.Count ? row[i] : "";
                sb.AppendLine($"    <{tag}>{SecurityElement.Escape(val)}</{tag}>");
            }
            sb.AppendLine("  </record>");
        }
        sb.Append("</records>");
        return sb.ToString();
    }

    private static string CsvToHtml(string path)
    {
        var rows = ReadCsv(path);
        if (rows.Count == 0) return WrapHtml("CSV", "");
        var sb = new StringBuilder(TableStyle);
        sb.Append("<thead><tr>");
        foreach (var h in rows[0]) sb.Append($"<th>{HtmlEnc(h)}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows.Skip(1))
        {
            sb.Append("<tr>");
            foreach (var c in row) sb.Append($"<td>{HtmlEnc(c)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return WrapHtml("CSV 변환 결과", sb.ToString());
    }

    private static string CsvToTxt(string path)
    {
        var rows = ReadCsv(path);
        return string.Join("\n", rows.Select(r => string.Join("\t", r)));
    }

    private static void CsvToXls(string inputPath, string outputPath)
    {
        var rows = ReadCsv(inputPath);
        var wb   = new HSSFWorkbook();
        var sh   = wb.CreateSheet("Sheet1");
        for (var i = 0; i < rows.Count; i++)
        {
            var row = sh.CreateRow(i);
            for (var j = 0; j < rows[i].Count; j++)
                row.CreateCell(j).SetCellValue(rows[i][j]);
        }
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        wb.Write(stream, false);
    }

    // ── JSON ───────────────────────────────────────────────────
    private static string JsonToXml(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<root>\n");
        AppendJsonAsXml(doc.RootElement, sb, "  ");
        sb.Append("</root>");
        return sb.ToString();
    }

    private static void AppendJsonAsXml(JsonElement el, StringBuilder sb, string indent)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject())
                {
                    var tag = SanitizeTag(p.Name);
                    if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        sb.AppendLine($"{indent}<{tag}>");
                        AppendJsonAsXml(p.Value, sb, indent + "  ");
                        sb.AppendLine($"{indent}</{tag}>");
                    }
                    else sb.AppendLine($"{indent}<{tag}>{SecurityElement.Escape(p.Value.ToString())}</{tag}>");
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                {
                    sb.AppendLine($"{indent}<item>");
                    AppendJsonAsXml(item, sb, indent + "  ");
                    sb.AppendLine($"{indent}</item>");
                }
                break;
            default:
                sb.AppendLine($"{indent}{SecurityElement.Escape(el.ToString())}");
                break;
        }
    }

    private static string JsonToCsv(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("CSV 변환은 JSON 배열만 지원합니다.");
        var items = doc.RootElement.EnumerateArray().ToList();
        if (items.Count == 0) return "";
        var keys = items.SelectMany(e => e.EnumerateObject().Select(p => p.Name)).Distinct().ToList();
        var sb = new StringBuilder(string.Join(",", keys.Select(k => $"\"{k}\"")) + "\n");
        foreach (var item in items)
            sb.AppendLine(string.Join(",", keys.Select(k =>
                item.TryGetProperty(k, out var v) ? $"\"{v.ToString().Replace("\"", "\"\"")}\"" : "\"\"")));
        return sb.ToString();
    }

    private static string JsonToHtml(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        var fmt = JsonSerializer.Serialize(doc.RootElement,
            new JsonSerializerOptions { WriteIndented = true });
        return WrapHtml("JSON 변환 결과",
            $"<pre style=\"font-family:monospace;background:#f8f8f8;padding:16px;border-radius:6px;overflow:auto\">{HtmlEnc(fmt)}</pre>");
    }

    private static string JsonToTxt(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    // ── XML ────────────────────────────────────────────────────
    private static string XmlToJson(string path)
    {
        var xdoc = XDocument.Load(path);
        var obj  = XelToObj(xdoc.Root!);
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object XelToObj(XElement el)
    {
        var children = el.Elements().ToList();
        if (!children.Any()) return (object)el.Value;
        var dict = new Dictionary<string, object>();
        foreach (var g in children.GroupBy(e => e.Name.LocalName))
            dict[g.Key] = g.Count() > 1 ? (object)g.Select(XelToObj).ToList() : XelToObj(g.First());
        return dict;
    }

    private static string XmlToCsv(string path)
    {
        var doc  = XDocument.Load(path);
        var recs = doc.Root?.Elements().ToList() ?? [];
        if (recs.Count == 0) return "";
        var headers = recs.SelectMany(r => r.Elements().Select(e => e.Name.LocalName)).Distinct().ToList();
        var sb = new StringBuilder(string.Join(",", headers.Select(h => $"\"{h}\"")) + "\n");
        foreach (var rec in recs)
            sb.AppendLine(string.Join(",", headers.Select(h =>
                $"\"{(rec.Element(h)?.Value ?? "").Replace("\"", "\"\"")}\"")));
        return sb.ToString();
    }

    private static string XmlToHtml(string path)
    {
        var xml = File.ReadAllText(path, Encoding.UTF8);
        return WrapHtml("XML 변환 결과",
            $"<pre style=\"font-family:monospace;background:#f8f8f8;padding:16px;border-radius:6px;overflow:auto\">{HtmlEnc(xml)}</pre>");
    }

    private static string XmlToTxt(string path)
    {
        var doc = XDocument.Load(path);
        var sb  = new StringBuilder();
        foreach (var t in doc.DescendantNodes().OfType<XText>())
            if (!string.IsNullOrWhiteSpace(t.Value)) sb.AppendLine(t.Value.Trim());
        return sb.ToString();
    }

    // ── XLS ────────────────────────────────────────────────────
    private static List<List<string>> ReadXls(string path)
    {
        using var stream = File.OpenRead(path);
        var wb    = new HSSFWorkbook(stream);
        var sheet = wb.GetSheetAt(0);
        var rows  = new List<List<string>>();
        for (var i = 0; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;
            var cells = new List<string>();
            for (var j = 0; j < row.LastCellNum; j++)
                cells.Add(row.GetCell(j)?.ToString() ?? "");
            rows.Add(cells);
        }
        return rows;
    }

    private static string XlsToCsv(string path) =>
        string.Join("\n", ReadXls(path).Select(r =>
            string.Join(",", r.Select(c => $"\"{c.Replace("\"", "\"\"")}\""))));

    private static string XlsToJson(string path)
    {
        var rows = ReadXls(path);
        if (rows.Count == 0) return "[]";
        var headers = rows[0];
        var recs = rows.Skip(1).Select(r =>
            headers.Select((h, i) => (h, v: i < r.Count ? r[i] : ""))
                   .ToDictionary(t => t.h, t => (object)t.v));
        return JsonSerializer.Serialize(recs, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string XlsToHtml(string path)
    {
        var rows = ReadXls(path);
        if (rows.Count == 0) return WrapHtml("XLS", "");
        var sb = new StringBuilder(TableStyle);
        sb.Append("<thead><tr>");
        foreach (var h in rows[0]) sb.Append($"<th>{HtmlEnc(h)}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows.Skip(1))
        {
            sb.Append("<tr>");
            foreach (var c in row) sb.Append($"<td>{HtmlEnc(c)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return WrapHtml("XLS 변환 결과", sb.ToString());
    }

    private static string XlsToTxt(string path) =>
        string.Join("\n", ReadXls(path).Select(r => string.Join("\t", r)));

    // ── 공통 헬퍼 ──────────────────────────────────────────────
    private static string WrapHtml(string title, string body) =>
        "<!DOCTYPE html>\n<html>\n<head>\n" +
        "  <meta charset=\"UTF-8\"/>\n" +
        $"  <title>{title}</title>\n" +
        "  <style>\n" +
        "    body { font-family: 'Malgun Gothic', sans-serif; margin: 32px; line-height: 1.7; color: #333; }\n" +
        "    table { border-collapse: collapse; width: 100%; }\n" +
        "    th, td { border: 1px solid #ddd; padding: 8px 12px; text-align: left; }\n" +
        "    th { background: #f0f0f0; font-weight: bold; }\n" +
        "    tr:nth-child(even) { background: #fafafa; }\n" +
        "  </style>\n</head>\n<body>\n" +
        body +
        "\n</body>\n</html>";

    private const string TableStyle =
        "<table style=\"border-collapse:collapse;width:100%\">";

    private static string Pre(string s) =>
        $"<pre style=\"white-space:pre-wrap;font-family:monospace\">{s}</pre>";

    private static string HtmlEnc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("\"", "&quot;");

    private static string SanitizeTag(string name)
    {
        var c = System.Text.RegularExpressions.Regex.Replace(
            name.Trim(), @"[^a-zA-Z0-9_\-]", "_");
        return string.IsNullOrEmpty(c) ? "_field"
             : char.IsDigit(c[0]) ? "_" + c : c;
    }
}
