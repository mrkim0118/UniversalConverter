using System.IO;
using System.Text;
using UniversalConverter.Services;
using Xunit;

namespace ConverterTests;

public class SharedServiceTests
{
    [Fact]
    public void ParseCsv_handles_escaped_quotes_and_newlines_in_quotes()
    {
        var rows = DocumentConversionService.ParseCsv("name,memo\r\nKim,\"He said \"\"hi\"\"\"\r\nLee,\"line1\nline2\"\r\n");

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "Kim", "He said \"hi\"" }, rows[1]);
        Assert.Equal(new[] { "Lee", "line1\nline2" }, rows[2]);
    }

    [Fact]
    public void ParseCsv_keeps_empty_fields_and_last_line_without_newline()
    {
        var rows = DocumentConversionService.ParseCsv("a,,c\n1,2,");

        Assert.Equal(new[] { "a", "", "c" }, rows[0]);
        Assert.Equal(new[] { "1", "2", "" }, rows[1]);
    }

    [Fact]
    public void ReadText_reads_utf8_and_cp949_korean()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var utf8 = Path.GetTempFileName();
        var cp949 = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(utf8, Encoding.UTF8.GetBytes("이름,나이"));
            File.WriteAllBytes(cp949, Encoding.GetEncoding(949).GetBytes("이름,나이"));

            Assert.Equal("이름,나이", DocumentConversionService.ReadText(utf8));
            Assert.Equal("이름,나이", DocumentConversionService.ReadText(cp949));
        }
        finally
        {
            File.Delete(utf8);
            File.Delete(cp949);
        }
    }

    [Theory]
    [InlineData("short", 10, 5)]          // fits whole
    [InlineData("hello world foo", 13, 12)] // breaks after the last space that fits
    [InlineData("가나다라마바사", 3, 3)]      // no spaces: break by character
    [InlineData("abc def", 3, 3)]          // break exactly at a space
    [InlineData("", 10, 0)]
    public void FitLength_wraps_at_word_or_char(string s, double width, int expected)
    {
        Assert.Equal(expected, DocumentConversionService.FitLength(s, t => t.Length, width));
    }

    [Fact]
    public void TxtToPdf_keeps_the_end_of_a_long_line()
    {
        var txt = Path.GetTempFileName() + ".txt";
        var pdf = Path.ChangeExtension(txt, ".pdf");
        try
        {
            var longLine = string.Concat(Enumerable.Repeat("word ", 200)) + "THE_END";
            File.WriteAllText(txt, longLine);

            DocumentConversionService.Convert(txt, pdf, "TXT", "PDF");

            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
            var text = string.Concat(doc.GetPages().Select(p => p.Text));
            Assert.Contains("THE_END", text);
            // Text past the right edge is still in the PDF but invisible, so check positions too.
            foreach (var page in doc.GetPages())
                Assert.All(page.GetWords(), w => Assert.True(w.BoundingBox.Right <= page.Width, $"'{w.Text}' is off the page"));
        }
        finally
        {
            File.Delete(txt);
            File.Delete(pdf);
        }
    }

    [Fact]
    public void FitLength_always_takes_one_char_even_if_too_wide()
    {
        Assert.Equal(1, DocumentConversionService.FitLength("WW", t => t.Length * 100, 10));
    }

    [Theory]
    [InlineData("JPG", true, true)]
    [InlineData("HEIC", true, false)]
    [InlineData("WEBP", true, false)]
    [InlineData("PNG", false, false)]
    [InlineData("PDF", false, true)]
    public void Image_settings_apply_only_to_matching_output(string to, bool quality, bool fillsTransparency)
    {
        Assert.Equal(quality, ImageConversionService.UsesQuality(to));
        Assert.Equal(fillsTransparency, ImageConversionService.FillsTransparency(to));
    }

    [Fact]
    public void Dpi_applies_only_to_vector_or_page_sources()
    {
        Assert.True(ImageConversionService.UsesDpi("PDF"));
        Assert.True(ImageConversionService.UsesDpi("svg"));
        Assert.False(ImageConversionService.UsesDpi("JPG"));
    }

    [Fact]
    public void Audio_and_video_settings_apply_only_to_matching_output()
    {
        Assert.True(AudioConversionService.UsesMp3Bitrate("MP3"));
        Assert.False(AudioConversionService.UsesMp3Bitrate("FLAC"));
        Assert.True(AudioConversionService.UsesBitDepth("AIFF"));
        Assert.False(AudioConversionService.UsesAacBitrate("WAV"));
        Assert.True(VideoConversionService.UsesCrf("WEBM"));
        Assert.False(VideoConversionService.UsesPreset("WEBM"));
        Assert.False(VideoConversionService.UsesCrf("AVI"));
    }

    // Every image output format must be accepted by the rules (MapFormat throws for unknown ones).
    [Fact]
    public void Image_setting_rules_cover_every_output_format()
    {
        foreach (var f in UniversalConverter.Models.ConversionOption.ImageOutputFormats)
        {
            ImageConversionService.UsesQuality(f);
            ImageConversionService.FillsTransparency(f);
        }
    }

    [Fact]
    public void Image_options_allow_jpg_to_jpg_but_no_other_same_format()
    {
        var all = UniversalConverter.Models.ConversionOption.GetAllImage();
        Assert.Contains(all, o => o.FromFormat == "JPG" && o.ToFormat == "JPG");
        Assert.DoesNotContain(all, o => o.FromFormat == "PNG" && o.ToFormat == "PNG");
    }

    [Fact]
    public void CommonTargets_for_mixed_sources_is_the_intersection()
    {
        var all = UniversalConverter.Models.ConversionOption.GetAllImage();
        var mixed = UniversalConverter.Views.ConverterView.CommonTargets(all, ["JPG", "PNG"]);

        Assert.Contains("JPG", mixed);        // PNG→JPG and JPG→JPG both exist
        Assert.DoesNotContain("PNG", mixed);  // PNG→PNG doesn't
        // JPG can become everything PNG can (plus JPG itself), so the shared set is exactly PNG's.
        Assert.Equal(UniversalConverter.Views.ConverterView.CommonTargets(all, ["PNG"]), mixed);
    }

    [Theory]
    [InlineData("frame=  100 fps=25 q=28.0 size=  512kB time=00:01:02.50 bitrate= 67.1kbits/s", 62.5)]
    [InlineData("size=N/A time=01:00:00.00 bitrate=N/A", 3600)]
    public void ParseTimeSeconds_reads_ffmpeg_progress(string line, double expected)
    {
        Assert.Equal(expected, FFmpegHelper.ParseTimeSeconds(line));
    }

    [Theory]
    [InlineData("Duration: 00:00:10.00, start: 0.000000")]
    [InlineData("time=N/A bitrate=N/A")]
    public void ParseTimeSeconds_returns_null_without_valid_time(string line)
    {
        Assert.Null(FFmpegHelper.ParseTimeSeconds(line));
    }
}
