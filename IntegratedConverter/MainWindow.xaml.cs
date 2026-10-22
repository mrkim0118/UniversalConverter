using System.Windows;
using UniversalConverter.Views;

namespace UniversalConverter;

public partial class MainWindow : Window
{
    private readonly HomeView _homeView;
    private readonly UniversalConverterView _imageView;
    private readonly AudioConverterView _audioView;
    private readonly VideoConverterView _videoView;
    private readonly DocumentConverterView _documentView;
    private readonly FontConverterView _fontView;

    public MainWindow()
    {
        InitializeComponent();

        _homeView = new HomeView();
        _homeView.NavigateRequested += ShowConverter;

        _imageView = new UniversalConverterView();
        _audioView = new AudioConverterView();
        _videoView = new VideoConverterView();
        _documentView = new DocumentConverterView();
        _fontView = new FontConverterView();

        _imageView.BackRequested += ShowHome;
        _audioView.BackRequested += ShowHome;
        _videoView.BackRequested += ShowHome;
        _documentView.BackRequested += ShowHome;
        _fontView.BackRequested += ShowHome;

        ShowHome();
    }

    private void ShowHome()
    {
        ContentArea.Content = _homeView;
    }

    private void ShowConverter(string target)
    {
        ContentArea.Content = target switch
        {
            "image" => _imageView,
            "audio" => _audioView,
            "video" => _videoView,
            "document" => _documentView,
            "font" => _fontView,
            _ => _homeView
        };
    }
}
