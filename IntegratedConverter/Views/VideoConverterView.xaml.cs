using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UniversalConverter.Models;
using UniversalConverter.Services;
using Microsoft.Win32;

namespace UniversalConverter.Views;

public partial class VideoConverterView : UserControl
{
    private const string DonateUrl = "https://www.paypal.com/ncp/payment/T2KAST2XQC6BS";
    private const string WebsiteUrl = "https://tech.trendarchive.io/";
    private readonly List<ConversionOption> _allOptions;
    private bool _isPlaceholder = true;
    private string _activeFilter = "전체";

    public event Action? BackRequested;

    public VideoConverterView()
    {
        InitializeComponent();
        _allOptions = ConversionOption.GetAllVideo();
        BuildFilterTabs();
        ApplyFilters();
        CheckFFmpeg();
        LanguageService.LanguageChanged += ApplyLanguage;
        ApplyLanguage(LanguageService.IsKorean);
    }

    private void ApplyLanguage(bool isKorean)
    {
        BackBtn.Content  = isKorean ? "← 돌아가기" : "← Back";
        HeaderTitle.Text = isKorean ? "🎬  비디오 포맷 변환기" : "🎬  Video Format Converter";
        LangBtn.Content = isKorean ? "EN" : "한국어";
        DonateBtn.Content = isKorean ? "기부" : "Donate";
        WebsiteBtn.Content = isKorean ? "홈페이지" : "Website";

        SettingsPanelTitle.Text    = isKorean ? "⚙  변환 설정"    : "⚙  Settings";
        CrfSettingLabel.Text       = isKorean ? "품질 (CRF)"      : "Quality (CRF)";
        PresetSettingLabel.Text    = isKorean ? "인코딩 속도"      : "Encoding Speed";
        AudioBitrateLabel.Text     = isKorean ? "오디오 비트레이트" : "Audio Bitrate";
        ResolutionSettingLabel.Text = isKorean ? "출력 해상도"     : "Resolution";

        var resIdx = ResolutionCombo.SelectedIndex;
        ResolutionCombo.Items.Clear();
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "",     Content = isKorean ? "원본 유지"   : "Original" });
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "360",  Content = "360p" });
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "480",  Content = "480p" });
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "720",  Content = "720p" });
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "1080", Content = "1080p" });
        ResolutionCombo.Items.Add(new ComboBoxItem { Tag = "2160", Content = "4K (2160p)" });
        ResolutionCombo.SelectedIndex = resIdx;

        if (_isPlaceholder)
            SearchBox.Text = GetPlaceholder();

        foreach (var child in FilterPanel.Children)
            if (child is Border b && b.Tag is string t && t == "전체" && b.Child is TextBlock tb)
                tb.Text = isKorean ? "전체" : "All";

        if (StatusText.Text is "변환할 형식을 선택하세요" or "Select a format to convert"
                             or "FFmpeg 설치가 필요합니다"  or "FFmpeg installation required")
            StatusText.Text = FFmpegHelper.FindFFmpeg() == null
                ? (isKorean ? "FFmpeg 설치가 필요합니다" : "FFmpeg installation required")
                : (isKorean ? "변환할 형식을 선택하세요"  : "Select a format to convert");

        ApplyFilters();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

    private void LangBtn_Click(object sender, RoutedEventArgs e) => LanguageService.Toggle();

    private void DonateBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(DonateUrl);

    private void WebsiteBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(WebsiteUrl);

    // ── FFmpeg 상태 확인 ──
    private void CheckFFmpeg()
    {
        bool ko = LanguageService.IsKorean;
        if (FFmpegHelper.FindFFmpeg() != null)
        {
            FFmpegBanner.Visibility = Visibility.Collapsed;
            StatusText.Text = ko ? "변환할 형식을 선택하세요" : "Select a format to convert";
        }
        else
        {
            FFmpegBanner.Visibility = Visibility.Visible;
            FFmpegBanner.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FEF3C7"));
            FFmpegStatusText.Text = ko
                ? "⚠️  비디오 변환에 FFmpeg가 필요합니다. 아래 버튼을 눌러 자동 설치하세요."
                : "⚠️  FFmpeg is required for video conversion. Click the button below to install automatically.";
            FFmpegStatusText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#92400E"));
            FFmpegActionBtn.Content = ko ? "FFmpeg 자동 설치" : "Install FFmpeg";
            FFmpegActionBtn.Visibility = Visibility.Visible;
            StatusText.Text = ko ? "FFmpeg 설치가 필요합니다" : "FFmpeg installation required";
        }
    }

    private async void FFmpegInstall_Click(object sender, RoutedEventArgs e)
    {
        bool ko = LanguageService.IsKorean;
        FFmpegActionBtn.IsEnabled = false;
        FFmpegActionBtn.Content = ko ? "설치 중..." : "Installing...";
        FFmpegBanner.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#ECFDF5"));
        FFmpegStatusText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#065F46"));

        var progress = new Progress<string>(msg =>
        {
            FFmpegStatusText.Text = $"⏳  {msg}";
            StatusText.Text = msg;
        });

        try
        {
            await FFmpegHelper.DownloadFFmpegAsync(progress);
            FFmpegBanner.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#D1FAE5"));
            FFmpegStatusText.Text = ko
                ? "✅  FFmpeg 설치 완료! 이제 비디오 변환이 가능합니다."
                : "✅  FFmpeg installed successfully! Video conversion is now available.";
            FFmpegStatusText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#065F46"));
            FFmpegActionBtn.Visibility = Visibility.Collapsed;
            StatusText.Text = ko ? "변환할 형식을 선택하세요" : "Select a format to convert";
        }
        catch (Exception ex)
        {
            FFmpegBanner.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FEE2E2"));
            FFmpegStatusText.Text = ko
                ? $"❌  설치 실패: {ex.Message}"
                : $"❌  Installation failed: {ex.Message}";
            FFmpegStatusText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#991B1B"));
            FFmpegActionBtn.Content = ko ? "재시도" : "Retry";
            FFmpegActionBtn.IsEnabled = true;
        }
    }

    // ── 필터 탭 ──
    private void BuildFilterTabs()
    {
        var tabs = new List<string> { "전체" };
        foreach (var fmt in ConversionOption.VideoFormats) tabs.Add($"→ {fmt}");
        foreach (var fmt in ConversionOption.VideoFormats) tabs.Add($"{fmt} →");

        foreach (var tab in tabs)
        {
            var isActive = tab == _activeFilter;
            var border = new Border
            {
                Style = (Style)FindResource(isActive ? "FilterBtnActive" : "FilterBtn"),
                Tag = tab,
            };
            border.Child = new TextBlock
            {
                Text = tab, FontSize = 12,
                FontFamily = new FontFamily("Malgun Gothic"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isActive ? Brushes.White
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
            };
            border.MouseLeftButtonDown += FilterTab_Click;
            FilterPanel.Children.Add(border);
        }
    }

    private void FilterTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: string tag }) return;
        _activeFilter = tag;
        foreach (var child in FilterPanel.Children)
        {
            if (child is Border b && b.Tag is string t)
            {
                var active = t == _activeFilter;
                b.Style = (Style)FindResource(active ? "FilterBtnActive" : "FilterBtn");
                if (b.Child is TextBlock tb)
                    tb.Foreground = active ? Brushes.White
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555"));
            }
        }
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var result = _allOptions.AsEnumerable();
        if (_activeFilter.StartsWith("→ "))
            result = result.Where(o => o.ToFormat == _activeFilter[2..].Trim());
        else if (_activeFilter.EndsWith(" →"))
            result = result.Where(o => o.FromFormat == _activeFilter[..^2].Trim());

        if (!_isPlaceholder && !string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var q = SearchBox.Text.Trim().ToUpper();
            result = result.Where(o => o.FromFormat.Contains(q) || o.ToFormat.Contains(q));
        }

        var filtered = result.ToList();
        CardsPanel.ItemsSource = filtered;
        bool ko = LanguageService.IsKorean;
        CountLabel.Text = ko ? $"{filtered.Count}개"              : $"{filtered.Count}";
        TotalLabel.Text = ko ? $"전체 {_allOptions.Count}개 변환 지원" : $"{_allOptions.Count} conversions supported";
    }

    // ── Search ──
    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (!_isPlaceholder) return;
        SearchBox.Text = "";
        SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"));
        _isPlaceholder = false;
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        SearchBox.Text = GetPlaceholder();
        SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
        _isPlaceholder = true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPlaceholder || CardsPanel == null) return;
        ApplyFilters();
    }

    // ── 설정 값 읽기 ──
    private void CrfSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CrfLabel != null)
            CrfLabel.Text = $"{(int)e.NewValue}";
    }

    private int GetCrf()
        => CrfSlider != null ? (int)CrfSlider.Value : 23;

    private string GetPreset()
        => (PresetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "medium";

    private int GetAudioBitrate()
    {
        var text = (AudioBitrateCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "192";
        return int.TryParse(text, out var v) ? v : 192;
    }

    private string GetResolution()
        => (ResolutionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

    // ── Convert ──
    private async void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ConversionOption option }) return;

        bool ko = LanguageService.IsKorean;

        if (FFmpegHelper.FindFFmpeg() == null)
        {
            MessageBox.Show(
                ko ? "FFmpeg가 설치되어 있지 않습니다.\n상단의 'FFmpeg 자동 설치' 버튼을 눌러주세요."
                   : "FFmpeg is not installed.\nPlease click the 'Install FFmpeg' button at the top.",
                ko ? "FFmpeg 필요" : "FFmpeg Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var openDlg = new OpenFileDialog
        {
            Title = ko ? $"{option.FromFormat} 파일 선택 (여러 개 동시 선택 가능)"
                       : $"Select {option.FromFormat} files (multiple selection allowed)",
            Filter = ko ? $"{option.FileFilter}|모든 파일 (*.*)|*.*"
                        : $"{option.FileFilter}|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (openDlg.ShowDialog() != true || openDlg.FileNames.Length == 0) return;

        var folderDlg = new OpenFolderDialog
        {
            Title = ko ? "변환된 파일을 저장할 폴더 선택" : "Select output folder"
        };
        if (folderDlg.ShowDialog() != true) return;
        var outDir = folderDlg.FolderName;

        var files = openDlg.FileNames;
        var total = files.Length;
        var success = 0;
        var errors = new List<string>();

        StatusText.Text = ko ? $"[{option.Title}] 변환 중... 0 / {total}"
                             : $"[{option.Title}] Converting... 0 / {total}";
        ConversionProgress.Value = 0;
        ConversionProgress.Visibility = Visibility.Visible;
        IsEnabled = false;

        var crf          = GetCrf();
        var preset       = GetPreset();
        var audioBitrate = GetAudioBitrate();
        var resolution   = GetResolution();

        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var fileIndex = success + errors.Count;

                var fileProgress = new Progress<double>(pct =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var overall = (fileIndex + pct / 100.0) / total * 100.0;
                        ConversionProgress.Value = overall;
                        StatusText.Text = ko
                            ? $"[{option.Title}] 변환 중... {fileIndex + 1} / {total}  ({(int)pct}%)"
                            : $"[{option.Title}] Converting... {fileIndex + 1} / {total}  ({(int)pct}%)";
                    });
                });

                try
                {
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    var outPath = Path.Combine(outDir, baseName + option.OutputExtension);
                    var counter = 1;
                    while (File.Exists(outPath))
                    {
                        outPath = Path.Combine(outDir, $"{baseName} ({counter}){option.OutputExtension}");
                        counter++;
                    }
                    VideoConversionService.Convert(file, outPath, option.ToFormat,
                        crf:          crf,
                        preset:       preset,
                        audioBitrate: audioBitrate,
                        resolution:   resolution,
                        progress:     fileProgress);
                    success++;
                }
                catch (Exception ex) { errors.Add($"  • {name}: {ex.Message}"); }

                var current = success + errors.Count;
                Dispatcher.Invoke(() =>
                {
                    ConversionProgress.Value = (double)current / total * 100;
                    StatusText.Text = ko ? $"[{option.Title}] 변환 중... {current} / {total}"
                                        : $"[{option.Title}] Converting... {current} / {total}";
                });
            }
        });

        ConversionProgress.Visibility = Visibility.Collapsed;
        IsEnabled = true;

        if (errors.Count > 0)
        {
            StatusText.Text = ko ? $"완료: {success}개 성공, {errors.Count}개 실패"
                                 : $"Done: {success} succeeded, {errors.Count} failed";
            MessageBox.Show(
                $"[{option.Title}]\n\n✅ {(ko ? "성공" : "Success")}: {success} / ❌ {(ko ? "실패" : "Failed")}: {errors.Count}\n\n" +
                string.Join("\n", errors),
                ko ? "변환 결과" : "Conversion Result",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            StatusText.Text = ko ? $"✅ {success}개 파일 변환 완료 ({option.Title})"
                                 : $"✅ {success} file(s) converted ({option.Title})";
            if (MessageBox.Show(
                    ko ? $"[{option.Title}]\n\n{success}개 파일이 성공적으로 변환되었습니다.\n\n저장 위치: {outDir}\n\n저장 폴더를 여시겠습니까?"
                       : $"[{option.Title}]\n\n{success} file(s) converted successfully.\n\nSaved to: {outDir}\n\nOpen output folder?",
                    ko ? "변환 완료" : "Conversion Complete",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Process.Start("explorer.exe", outDir);
        }
    }

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

    private static string GetPlaceholder() =>
        LanguageService.IsKorean
            ? "변환 형식 검색  (예: MP4, AVI, WEBM ...)"
            : "Search format  (e.g. MP4, AVI, WEBM ...)";
}
