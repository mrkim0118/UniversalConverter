using System.Windows;
using System.Reflection;
using UniversalConverter.Views;

namespace UniversalConverter;

public partial class MainWindow : Window
{
    private readonly VideoConverterView _videoView;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"{Title} v{GetDisplayVersion()}";

        _videoView = new VideoConverterView();

        ContentArea.Content = _videoView;
    }

    private static string GetDisplayVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "1.0.4" : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }
}
