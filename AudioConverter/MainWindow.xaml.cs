using System.Windows;
using System.Reflection;
using UniversalConverter.Views;

namespace UniversalConverter;

public partial class MainWindow : Window
{
    private readonly AudioConverterView _audioView;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"{Title} v{GetDisplayVersion()}";

        _audioView = new AudioConverterView();

        ContentArea.Content = _audioView;
    }

    private static string GetDisplayVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "1.0.6" : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }
}
