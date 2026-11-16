using System.Windows;
using System.Windows.Media;
using UniversalConverter.Models;
using UniversalConverter.Services;
using UniversalConverter.Views;

namespace UniversalConverter;

public partial class MainWindow : Window
{
    private readonly HomeView _homeView;
    private readonly Dictionary<string, ConverterView> _converters;

    public MainWindow()
    {
        InitializeComponent();

        var (imageUi, imageConvert) = SettingsPanel.Image();
        var (audioUi, audioConvert) = SettingsPanel.Audio();
        var (videoUi, videoConvert) = SettingsPanel.Video();

        _converters = new()
        {
            ["image"] = new(("이미지 변환기", "Image converter"), Rgb(0x6D28D9),
                ConversionOption.GetAllImage(), imageUi, imageConvert),
            ["audio"] = new(("오디오 변환기", "Audio converter"), Rgb(0x1D4ED8),
                ConversionOption.GetAllAudio(), audioUi, audioConvert, needsFFmpeg: true),
            ["video"] = new(("비디오 변환기", "Video converter"), Rgb(0x047857),
                ConversionOption.GetAllVideo(), videoUi, videoConvert, needsFFmpeg: true),
            ["document"] = new(("문서 변환기", "Document converter"), Rgb(0xB45309),
                ConversionOption.GetAllDocument(), null,
                () => (i, o, opt, _) => DocumentConversionService.Convert(i, o, opt.FromFormat, opt.ToFormat)),
            ["font"] = new(("폰트 변환기", "Font converter"), Rgb(0xBE185D),
                ConversionOption.GetAllFont(), null,
                () => (i, o, opt, _) => FontConversionService.Convert(i, o, opt.FromFormat, opt.ToFormat)),
        };
        foreach (var view in _converters.Values) view.BackRequested += ShowHome;

        _homeView = new HomeView();
        _homeView.NavigateRequested += ShowConverter;
        _homeView.FilesDropped += OpenFiles;
        ShowHome();
    }

    private static Color Rgb(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private void ShowHome() => ContentArea.Content = _homeView;

    private void ShowConverter(string target) =>
        ContentArea.Content = _converters.TryGetValue(target, out var view) ? view : _homeView;

    // Opens the converter that understands the first supported file and hands it all the files.
    private void OpenFiles(string[] files)
    {
        var view = files.Select(f => _converters.Values.FirstOrDefault(c => c.CanHandle(f))).FirstOrDefault(v => v != null);
        if (view == null)
        {
            var ko = LanguageService.IsKorean;
            MessageBox.Show(ko ? "지원하지 않는 파일 형식입니다." : "This file type isn't supported.",
                ko ? "변환할 수 없음" : "Can't convert", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        view.LoadFiles(files);
        ContentArea.Content = view;
    }
}
