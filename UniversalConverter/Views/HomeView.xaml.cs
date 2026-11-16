using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UniversalConverter.Services;

namespace UniversalConverter.Views;

public partial class HomeView : UserControl
{
    public event Action<string>? NavigateRequested;
    public event Action<string[]>? FilesDropped;

    public HomeView()
    {
        InitializeComponent();
        LanguageService.LanguageChanged += ApplyLanguage;
        ApplyLanguage(LanguageService.IsKorean);
    }

    private void ApplyLanguage(bool isKorean)
    {
        LangBtn.Content = isKorean ? "EN" : "한국어";
        HeroTitle.Text = isKorean ? "유니버설 컨버터" : "Universal Converter";
        HeroSubtitle.Text = isKorean ? "이미지 · 오디오 · 비디오 · 문서 · 폰트" : "Image · Audio · Video · Document · Font";
        DropTitle.Text = isKorean ? "변환할 파일을 여기에 끌어다 놓으세요" : "Drop files here to convert";
        System.Windows.Automation.AutomationProperties.SetName(DropZone, isKorean ? "변환할 파일 고르기" : "Choose files to convert");
        DropHint.Text = isKorean ? "형식을 알아서 찾아 맞는 변환기를 엽니다. 클릭해서 고를 수도 있습니다."
                                 : "We'll open the right converter for you. You can also click to choose.";
        SectionTitle.Text = isKorean ? "변환기 선택" : "Choose a converter";
        ImageTitle.Text = isKorean ? "이미지" : "Image";
        AudioTitle.Text = isKorean ? "오디오" : "Audio";
        VideoTitle.Text = isKorean ? "비디오" : "Video";
        DocumentTitle.Text = isKorean ? "문서" : "Document";
        FontTitle.Text = isKorean ? "폰트" : "Font";
    }

    private void LangBtn_Click(object sender, RoutedEventArgs e) => LanguageService.Toggle();

    private void ImageBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("image");

    private void AudioBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("audio");

    private void VideoBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("video");

    private void DocumentBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("document");

    private void FontBtn_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke("font");

    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        DropZone.Tag = "drag";
        e.Handled = true;
    }

    private void Root_DragLeave(object sender, DragEventArgs e) => DropZone.Tag = null;

    private void Root_Drop(object sender, DragEventArgs e)
    {
        DropZone.Tag = null;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) FilesDropped?.Invoke(files);
    }

    private void PickFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = LanguageService.IsKorean ? "변환할 파일 선택" : "Select files to convert",
            Multiselect = true,
        };
        if (dlg.ShowDialog() == true) FilesDropped?.Invoke(dlg.FileNames);
    }
}
