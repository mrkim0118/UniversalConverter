using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UniversalConverter.Services;

public sealed class UpdateDownloadProgressWindow : Window
{
    private readonly TextBlock _messageText;
    private readonly TextBlock _percentText;
    private readonly ProgressBar _progressBar;

    public UpdateDownloadProgressWindow(Window? owner = null)
    {
        Owner = owner?.IsVisible == true ? owner : null;
        Title = "Downloading Update";
        Width = 380;
        Height = 170;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        WindowStartupLocation = Owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var layout = new Grid
        {
            Margin = new Thickness(20)
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _messageText = new TextBlock
        {
            Text = "Downloading update... 0%",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(_messageText, 0);

        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 18,
            Value = 0
        };
        Grid.SetRow(_progressBar, 1);

        _percentText = new TextBlock
        {
            Text = "0%",
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = Brushes.DimGray
        };
        Grid.SetRow(_percentText, 2);

        layout.Children.Add(_messageText);
        layout.Children.Add(_progressBar);
        layout.Children.Add(_percentText);

        Content = layout;
    }

    public void ReportProgress(int progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ReportProgress(progress));
            return;
        }

        var clamped = Math.Clamp(progress, 0, 100);
        _progressBar.Value = clamped;
        _messageText.Text = $"Downloading update... {clamped}%";
        _percentText.Text = $"{clamped}%";
    }

    public void CloseSafely()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(CloseSafely);
            return;
        }

        Close();
    }
}
