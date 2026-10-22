namespace UniversalConverter.Models;

public class ConversionOption
{
    public string FromFormat { get; }
    public string ToFormat { get; }
    public string Title => $"{FromFormat} → {ToFormat}";
    public string Description => $"{FromFormat} 파일을 {ToFormat} 형식으로 변환";

    public ConversionOption(string from, string to)
    {
        FromFormat = from;
        ToFormat = to;
    }

    public string FileFilter
    {
        get
        {
            var ext = FromFormat.ToLower();
            return ext switch
            {
                // Image
                "jpg"   => "JPG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                "heic"  => "HEIC Files (*.heic;*.heif)|*.heic;*.heif",
                "tiff"  => "TIFF Files (*.tiff;*.tif)|*.tiff;*.tif",
                "svg"   => "SVG Files (*.svg;*.svgz)|*.svg;*.svgz",
                // Audio
                "aiff"  => "AIFF Files (*.aiff;*.aif)|*.aiff;*.aif",
                "aac"   => "AAC Files (*.aac;*.m4a)|*.aac;*.m4a",
                // Video
                "mpeg"  => "MPEG Files (*.mpeg;*.mpg)|*.mpeg;*.mpg",
                // Document
                "doc"   => "Word 97-2003 문서 (*.doc)|*.doc",
                "docx"  => "Word 문서 (*.docx)|*.docx",
                "pdf"   => "PDF 파일 (*.pdf)|*.pdf",
                "html"  => "HTML 파일 (*.html;*.htm)|*.html;*.htm",
                "rtf"   => "서식 있는 텍스트 (*.rtf)|*.rtf",
                "txt"   => "텍스트 파일 (*.txt)|*.txt",
                "csv"   => "CSV 파일 (*.csv)|*.csv",
                "json"  => "JSON 파일 (*.json)|*.json",
                "xml"   => "XML 파일 (*.xml)|*.xml",
                "xls"   => "Excel 97-2003 (*.xls)|*.xls",
                // Font
                "ttf"   => "TrueType 폰트 (*.ttf)|*.ttf",
                "otf"   => "OpenType 폰트 (*.otf)|*.otf",
                "woff"  => "WOFF 폰트 (*.woff)|*.woff",
                "woff2" => "WOFF2 폰트 (*.woff2)|*.woff2",
                "eot"   => "EOT 폰트 (*.eot)|*.eot",
                // 기본
                _ => $"{FromFormat} Files (*.{ext})|*.{ext}"
            };
        }
    }

    public string OutputExtension => ToFormat.ToLower() switch
    {
        "jpg"  => ".jpg",
        "tiff" => ".tiff",
        "mpeg" => ".mpeg",
        "html" => ".html",
        _      => $".{ToFormat.ToLower()}"
    };

    // ════════════════════════════════════════════════════════
    //  이미지 포맷
    // ════════════════════════════════════════════════════════
    public static readonly string[] ImageInputFormats =
        ["HEIC", "JPG", "PDF", "PSD", "WEBP", "PNG", "GIF", "TIFF", "BMP", "SVG", "AVIF", "ICO"];

    public static readonly string[] ImageOutputFormats =
        ["JPG", "PNG", "WEBP", "GIF", "TIFF", "BMP", "AVIF", "ICO", "PDF", "PSD", "HEIC"];

    public static List<ConversionOption> GetAllImage()
    {
        var list = new List<ConversionOption>();
        foreach (var from in ImageInputFormats)
            foreach (var to in ImageOutputFormats)
                if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                    list.Add(new ConversionOption(from, to));
        return list;
    }

    // ════════════════════════════════════════════════════════
    //  오디오 포맷
    // ════════════════════════════════════════════════════════
    public static readonly string[] AudioFormats =
        ["MP3", "FLAC", "WAV", "AIFF", "AAC"];

    public static List<ConversionOption> GetAllAudio()
    {
        var list = new List<ConversionOption>();
        foreach (var from in AudioFormats)
            foreach (var to in AudioFormats)
                if (from != to) list.Add(new ConversionOption(from, to));
        return list;
    }

    // ════════════════════════════════════════════════════════
    //  비디오 포맷
    // ════════════════════════════════════════════════════════
    public static readonly string[] VideoFormats =
        ["MP4", "AVI", "WEBM", "MKV", "FLV", "MPEG"];

    public static List<ConversionOption> GetAllVideo()
    {
        var list = new List<ConversionOption>();
        foreach (var from in VideoFormats)
            foreach (var to in VideoFormats)
                if (from != to) list.Add(new ConversionOption(from, to));
        return list;
    }

    // ════════════════════════════════════════════════════════
    //  문서 포맷 (지원 변환 화이트리스트)
    // ════════════════════════════════════════════════════════
    public static readonly string[] DocumentFormats =
        ["PDF", "DOCX", "CSV", "XML", "HTML", "RTF", "TXT", "JSON", "XLS"];

    private static readonly (string From, string To)[] DocConversions =
    [
        // PDF 입력
        ("PDF", "TXT"), ("PDF", "HTML"),
        // DOCX 입력
        ("DOCX", "TXT"), ("DOCX", "HTML"), ("DOCX", "PDF"),
        // HTML 입력
        ("HTML", "TXT"), ("HTML", "PDF"), ("HTML", "DOCX"),
        // TXT 입력
        ("TXT", "HTML"), ("TXT", "PDF"), ("TXT", "DOCX"),
        // RTF 입력
        ("RTF", "TXT"), ("RTF", "HTML"), ("RTF", "PDF"),
        // CSV 입력
        ("CSV", "JSON"), ("CSV", "XML"), ("CSV", "HTML"), ("CSV", "TXT"), ("CSV", "XLS"),
        // JSON 입력
        ("JSON", "XML"), ("JSON", "CSV"), ("JSON", "HTML"), ("JSON", "TXT"),
        // XML 입력
        ("XML", "JSON"), ("XML", "CSV"), ("XML", "HTML"), ("XML", "TXT"),
        // XLS 입력
        ("XLS", "CSV"), ("XLS", "JSON"), ("XLS", "HTML"), ("XLS", "TXT"),
    ];

    public static List<ConversionOption> GetAllDocument() =>
        DocConversions.Select(t => new ConversionOption(t.From, t.To)).ToList();

    // ════════════════════════════════════════════════════════
    //  폰트 포맷
    // ════════════════════════════════════════════════════════
    public static readonly string[] FontFormats =
        ["TTF", "OTF", "WOFF", "WOFF2", "EOT"];

    public static List<ConversionOption> GetAllFont()
    {
        var list = new List<ConversionOption>();
        foreach (var from in FontFormats)
            foreach (var to in FontFormats)
                if (from != to) list.Add(new ConversionOption(from, to));
        return list;
    }
}
