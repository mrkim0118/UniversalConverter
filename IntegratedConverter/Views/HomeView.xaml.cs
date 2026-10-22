using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using UniversalConverter.Services;

namespace UniversalConverter.Views;

public partial class HomeView : UserControl
{
    private const string DonateUrl = "https://www.paypal.com/ncp/payment/T2KAST2XQC6BS";
    private const string WebsiteUrl = "https://tech.trendarchive.io/";

    public event Action<string>? NavigateRequested;

    public HomeView()
    {
        InitializeComponent();
        LanguageService.LanguageChanged += ApplyLanguage;
        ApplyLanguage(LanguageService.IsKorean);
    }

    private void ApplyLanguage(bool isKorean)
    {
        LangBtn.Content = isKorean ? "EN" : "한국어";
        DonateBtn.Content = isKorean ? "기부" : "Donate";
        WebsiteBtn.Content = isKorean ? "홈페이지" : "Website";

        HeroTitle.Text = isKorean ? "유니버설 컨버터" : "Universal Converter";
        HeroSubtitle.Text = isKorean
            ? "이미지, 오디오, 비디오, 문서, 폰트 변환기를 하나의 프로그램에서 바로 열 수 있습니다."
            : "Open one workspace for image, audio, video, document, and font conversion.";
        SectionTitle.Text = isKorean ? "변환기 선택" : "Choose a converter";
        SectionSubtitle.Text = isKorean
            ? "각 변환기의 전용 옵션은 그대로 유지되고, 언제든 다시 이 홈으로 돌아올 수 있습니다."
            : "Each tool keeps its own specialized options, and you can return here anytime.";

        ImageTitle.Text = isKorean ? "이미지 변환기" : "Image Converter";
        ImageDesc.Text = isKorean
            ? "JPG, PNG, WEBP, GIF, TIFF, AVIF, PDF 등 이미지 포맷 변환."
            : "JPG, PNG, WEBP, GIF, TIFF, AVIF, PDF and more.";
        AudioTitle.Text = isKorean ? "오디오 변환기" : "Audio Converter";
        AudioDesc.Text = isKorean
            ? "MP3, FLAC, WAV, AIFF, AAC 변환과 FFmpeg 자동 설치 지원."
            : "MP3, FLAC, WAV, AIFF, AAC with FFmpeg-powered conversion.";
        VideoTitle.Text = isKorean ? "비디오 변환기" : "Video Converter";
        VideoDesc.Text = isKorean
            ? "MP4, AVI, WEBM, MKV, FLV, MPEG 변환과 화질/해상도 설정 지원."
            : "MP4, AVI, WEBM, MKV, FLV, MPEG with quality and resolution controls.";
        DocumentTitle.Text = isKorean ? "문서 변환기" : "Document Converter";
        DocumentDesc.Text = isKorean
            ? "PDF, DOCX, HTML, TXT, CSV, JSON, XML, XLS 등 문서 포맷 변환."
            : "PDF, DOCX, HTML, TXT, CSV, JSON, XML, XLS and more.";
        FontTitle.Text = isKorean ? "폰트 변환기" : "Font Converter";
        FontDesc.Text = isKorean
            ? "TTF, OTF, WOFF, WOFF2, EOT 폰트 변환을 한곳에서 처리."
            : "TTF, OTF, WOFF, WOFF2, EOT conversion in one place.";
    }

    private void LangBtn_Click(object sender, RoutedEventArgs e) => LanguageService.Toggle();

    private void DonateBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(DonateUrl);

    private void WebsiteBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(WebsiteUrl);

    private void ImageBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("image");

    private void AudioBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("audio");

    private void VideoBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("video");

    private void DocumentBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("document");

    private void FontBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("font");

    private static void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            var isKorean = LanguageService.IsKorean;
            MessageBox.Show(
                isKorean ? $"링크를 열 수 없습니다.\n{ex.Message}" : $"Unable to open the link.\n{ex.Message}",
                isKorean ? "열기 실패" : "Open Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
