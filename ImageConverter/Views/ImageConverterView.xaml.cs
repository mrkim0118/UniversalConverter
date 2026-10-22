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

public partial class UniversalConverterView : UserControl
{
    private const string DonateUrl = "https://www.paypal.com/ncp/payment/T2KAST2XQC6BS";
    private const string WebsiteUrl = "https://tech.trendarchive.io/";
    private readonly List<ConversionOption> _allOptions;
    private bool _isPlaceholder = true;
    private string _activeFilter = "전체";

    public UniversalConverterView()
    {
        InitializeComponent();
        _allOptions = ConversionOption.GetAllImage();
        BuildFilterTabs();
        ApplyFilters();
        LanguageService.LanguageChanged += ApplyLanguage;
        ApplyLanguage(LanguageService.IsKorean);
    }

    private void ApplyLanguage(bool isKorean)
    {
        HeaderTitle.Text = isKorean ? "🖼  이미지 포맷 변환기" : "🖼  Image Format Converter";
        LangBtn.Content = isKorean ? "EN" : "한국어";
        DonateBtn.Content = isKorean ? "기부" : "Donate";
        WebsiteBtn.Content = isKorean ? "홈페이지" : "Website";

        SettingsPanelTitle.Text  = isKorean ? "⚙  변환 설정"            : "⚙  Settings";
        QualitySettingLabel.Text = isKorean ? "출력 품질"                : "Quality";
        QualityAppliesLabel.Text = isKorean ? "JPG · WebP · AVIF 에만 적용" : "Applies to JPG · WebP · AVIF only";
        BgColorSettingLabel.Text = isKorean ? "투명 배경색"              : "BG Color";

        var bgIdx = BgColorCombo.SelectedIndex;
        BgColorCombo.Items.Clear();
        BgColorCombo.Items.Add(new ComboBoxItem { Content = isKorean ? "흰색" : "White" });
        BgColorCombo.Items.Add(new ComboBoxItem { Content = isKorean ? "검정" : "Black" });
        BgColorCombo.SelectedIndex = bgIdx;

        if (_isPlaceholder)
            SearchBox.Text = GetPlaceholder();

        foreach (var child in FilterPanel.Children)
            if (child is Border b && b.Tag is string t && t == "전체" && b.Child is TextBlock tb)
                tb.Text = isKorean ? "전체" : "All";

        if (StatusText.Text is "변환할 형식을 선택하세요" or "Select a format to convert")
            StatusText.Text = isKorean ? "변환할 형식을 선택하세요" : "Select a format to convert";

        ApplyFilters();
    }

    private void LangBtn_Click(object sender, RoutedEventArgs e) => LanguageService.Toggle();

    private void DonateBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(DonateUrl);

    private void WebsiteBtn_Click(object sender, RoutedEventArgs e) => OpenExternalUrl(WebsiteUrl);

    // ── 필터 탭 ──
    private void BuildFilterTabs()
    {
        var tabs = new List<string> { "전체" };
        foreach (var fmt in ConversionOption.ImageOutputFormats) tabs.Add($"→ {fmt}");
        foreach (var fmt in ConversionOption.ImageInputFormats) tabs.Add($"{fmt} →");

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

    // ── 설정 슬라이더 ──
    private void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QualityLabel != null)
            QualityLabel.Text = $"{(int)e.NewValue}%";
    }

    private int GetDpi()
    {
        var text = (DpiCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "300";
        return int.TryParse(text, out var v) ? v : 300;
    }

    private string GetBgColor() => BgColorCombo.SelectedIndex == 1 ? "Black" : "White";

    // ── Convert ──
    private async void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ConversionOption option }) return;

        bool ko = LanguageService.IsKorean;

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

        var quality = (int)QualitySlider.Value;
        var dpi     = GetDpi();
        var bgColor = GetBgColor();

        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
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
                    ImageConversionService.Convert(file, outPath, option.ToFormat,
                        quality: quality,
                        dpi:     dpi,
                        bgColor: bgColor);
                    success++;
                }
                catch (Exception ex) { errors.Add($"  • {name}: {ex.Message}"); }

                var current = success + errors.Count;
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = ko ? $"[{option.Title}] 변환 중... {current} / {total}"
                                        : $"[{option.Title}] Converting... {current} / {total}";
                    ConversionProgress.Value = (double)current / total * 100;
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
            ? "변환 형식 검색  (예: JPG, PNG, HEIC ...)"
            : "Search format  (e.g. JPG, PNG, HEIC ...)";
}
