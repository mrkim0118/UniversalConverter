using System.IO;
using ImageMagick;

namespace UniversalConverter.Services;

public static class ImageConversionService
{
    /// <summary>
    /// 이미지 파일을 지정된 형식으로 변환합니다.
    /// </summary>
    /// <param name="quality">출력 품질 1-100 (JPG·WebP·AVIF에 적용)</param>
    /// <param name="dpi">PDF·SVG·EPS 래스터화 해상도</param>
    /// <param name="bgColor">투명 배경 처리 색상 ("White" 또는 "Black")</param>
    public static void Convert(string inputPath, string outputPath, string toFormat,
        int quality = 90, int dpi = 300, string bgColor = "White")
    {
        var format  = MapFormat(toFormat);
        var fromExt = Path.GetExtension(inputPath).TrimStart('.').ToUpper();

        var readSettings = new MagickReadSettings();
        if (UsesDpi(fromExt))
            readSettings.Density = new Density(dpi, dpi);

        using var image = new MagickImage(inputPath, readSettings);

        // ── 투명 배경 처리 (JPG, BMP, PDF 등 투명 미지원 포맷) ──
        if (FillsTransparency(toFormat))
        {
            image.BackgroundColor = bgColor == "Black" ? MagickColors.Black : MagickColors.White;
            image.Alpha(AlphaOption.Remove);
        }

        // ── ICO: 256×256 이하로 리사이즈 ──
        if (format == MagickFormat.Ico)
        {
            if (image.Width > 256 || image.Height > 256)
                image.Resize(new MagickGeometry(256, 256) { IgnoreAspectRatio = false, Greater = true });
        }

        // ── 품질 설정 (손실 포맷만 적용) ──
        if (UsesQuality(toFormat))
            image.Quality = (uint)Math.Clamp(quality, 1, 100);

        image.Write(outputPath, format);
    }

    // Which settings matter for a conversion; the UI uses the same rules to disable the rest.

    /// <summary>Quality applies to lossy output formats.</summary>
    public static bool UsesQuality(string toFormat) =>
        MapFormat(toFormat) is MagickFormat.Jpeg or MagickFormat.WebP or MagickFormat.Avif or MagickFormat.Heic;

    /// <summary>The background colour fills transparency for output formats that can't keep it.</summary>
    public static bool FillsTransparency(string toFormat) =>
        MapFormat(toFormat) is MagickFormat.Jpeg or MagickFormat.Bmp or MagickFormat.Pdf;

    /// <summary>DPI applies when the source is vector/page based and has to be rasterised.</summary>
    public static bool UsesDpi(string fromFormat) =>
        fromFormat.ToUpperInvariant() is "PDF" or "EPS" or "SVG" or "SVGZ";

    private static MagickFormat MapFormat(string fmt) => fmt.ToUpper() switch
    {
        "JPG" or "JPEG" => MagickFormat.Jpeg,
        "PNG"           => MagickFormat.Png,
        "BMP"           => MagickFormat.Bmp,
        "GIF"           => MagickFormat.Gif,
        "TIFF"          => MagickFormat.Tiff,
        "WEBP"          => MagickFormat.WebP,
        "AVIF"          => MagickFormat.Avif,
        "ICO"           => MagickFormat.Ico,
        "SVG"           => MagickFormat.Svg,
        "PDF"           => MagickFormat.Pdf,
        "PSD"           => MagickFormat.Psd,
        "HEIC"          => MagickFormat.Heic,
        _ => throw new NotSupportedException($"지원하지 않는 형식: {fmt}")
    };
}
