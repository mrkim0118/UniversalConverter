using System.Windows;
using System.Reflection;
using UniversalConverter.Views;

namespace UniversalConverter;

public partial class MainWindow : Window
{
    private readonly FontConverterView _fontView;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"{Title} v{GetDisplayVersion()}";

        _fontView = new FontConverterView();

        ContentArea.Content = _fontView;
    }

    private static string GetDisplayVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "1.0.5" : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }
}
