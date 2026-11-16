using System.Windows;
using System.Windows.Controls;
using UniversalConverter.Services;

namespace UniversalConverter.Views;

/// <summary>A row of labelled settings whose texts follow the app language.</summary>
public class SettingsPanel : WrapPanel
{
    public record Item(string Ko, string En, object Value);

    private readonly List<Action<bool>> _relabel = [];

    public SettingsPanel()
    {
        VerticalAlignment = VerticalAlignment.Center;
        LanguageService.LanguageChanged += ko => _relabel.ForEach(a => a(ko));
    }

    private void Relabel(Action<bool> apply)
    {
        _relabel.Add(apply);
        apply(LanguageService.IsKorean);
    }

    private StackPanel Group(string ko, string en, string? tipKo, string? tipEn)
    {
        var label = new TextBlock { Style = (Style)FindResource("Label") };
        Relabel(k =>
        {
            label.Text = k ? ko : en;
            label.ToolTip = tipKo == null ? null : (k ? tipKo : tipEn);
        });
        var group = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 24, 4) };
        group.Children.Add(label);
        Children.Add(group);
        return group;
    }

    /// <summary>Dropdown; returns a getter for the selected item's value.</summary>
    public Func<T> Combo<T>(string ko, string en, Item[] items, int selected, string? tipKo = null, string? tipEn = null)
    {
        var combo = new ComboBox { MinWidth = 90, FontSize = 13 };
        foreach (var it in items) combo.Items.Add(new ComboBoxItem { Tag = it.Value });
        combo.SelectedIndex = selected;
        Relabel(k =>
        {
            for (var i = 0; i < items.Length; i++) ((ComboBoxItem)combo.Items[i]).Content = k ? items[i].Ko : items[i].En;
        });
        AutomationProperties(combo, ko, en);
        Group(ko, en, tipKo, tipEn).Children.Add(combo);
        return () => (T)((ComboBoxItem)combo.SelectedItem).Tag;
    }

    /// <summary>Segmented choice; returns a getter for the checked item's value.</summary>
    public Func<T> Segments<T>(string ko, string en, Item[] items, int selected, string? tipKo = null, string? tipEn = null)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var buttons = items.Select((it, i) => new RadioButton
        {
            Style = (Style)FindResource("Segment"),
            GroupName = ko + GetHashCode(),
            IsChecked = i == selected,
            Tag = it.Value,
        }).ToList();
        buttons.ForEach(b => row.Children.Add(b));
        Relabel(k =>
        {
            for (var i = 0; i < items.Length; i++) buttons[i].Content = k ? items[i].Ko : items[i].En;
        });
        Group(ko, en, tipKo, tipEn).Children.Add(row);
        return () => (T)buttons.First(b => b.IsChecked == true).Tag;
    }

    /// <summary>Whole-number box; the getter throws ArgumentException (shown to the user) when out of range.</summary>
    public Func<int> Number(string ko, string en, int value, int min, int max, string unit, string? tipKo = null, string? tipEn = null)
    {
        var box = new TextBox
        {
            Text = value.ToString(), Width = 64, MaxLength = max.ToString().Length,
            VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center,
            FontSize = 13,
        };
        box.PreviewTextInput += (_, e) => e.Handled = !e.Text.All(char.IsDigit);
        DataObject.AddPastingHandler(box, (_, e) =>
        {
            if (e.DataObject.GetData(typeof(string)) is not string s || !s.All(char.IsDigit)) e.CancelCommand();
        });
        bool Valid() => int.TryParse(box.Text, out var v) && v >= min && v <= max;
        box.TextChanged += (_, _) =>
        {
            if (Valid()) box.ClearValue(Control.BorderBrushProperty);
            else box.SetResourceReference(Control.BorderBrushProperty, "SystemFillColorCriticalBrush");
        };
        AutomationProperties(box, ko, en);

        var group = Group(ko, en, tipKo, tipEn);
        group.Children.Add(box);
        group.Children.Add(new TextBlock { Text = unit, Style = (Style)FindResource("Label"), Margin = new Thickness(6, 0, 0, 0) });
        return () => Valid()
            ? int.Parse(box.Text)
            : throw new ArgumentException(LanguageService.IsKorean
                ? $"{ko} 값은 {min}~{max} 사이의 숫자로 입력하세요."
                : $"{en} must be a number from {min} to {max}.");
    }

    private void AutomationProperties(FrameworkElement el, string ko, string en) =>
        Relabel(k => System.Windows.Automation.AutomationProperties.SetName(el, k ? ko : en));

    private static Item[] Numbers(string unit, params int[] values) =>
        values.Select(v => new Item($"{v} {unit}", $"{v} {unit}", v)).ToArray();

    // ── The three converters that have settings ──

    public static (FrameworkElement, Func<ConvertFile>) Image()
    {
        var p = new SettingsPanel();
        var quality = p.Number("품질", "Quality", 90, 1, 100, "%",
            "1~100. JPG · WEBP · AVIF로 저장할 때만 적용됩니다", "1-100. Applies to JPG, WEBP and AVIF only");
        var bg = p.Combo<string>("투명 배경", "Transparent background",
            [new("흰색", "White", "White"), new("검정", "Black", "Black")], 0,
            "JPG·BMP처럼 투명을 지원하지 않는 형식으로 바꿀 때 채울 색", "Fill colour for formats without transparency, such as JPG or BMP");
        var dpi = p.Combo<int>("PDF·SVG 해상도", "PDF/SVG resolution", Numbers("DPI", 72, 96, 150, 300, 600), 3,
            "PDF·SVG·EPS를 이미지로 바꿀 때의 해상도", "Resolution used when rasterising PDF, SVG or EPS");
        return (p, () =>
        {
            int q = quality(), d = dpi();
            var b = bg();
            return (i, o, opt, _) => ImageConversionService.Convert(i, o, opt.ToFormat, quality: q, dpi: d, bgColor: b);
        });
    }

    public static (FrameworkElement, Func<ConvertFile>) Audio()
    {
        var p = new SettingsPanel();
        var mp3 = p.Combo<int>("MP3 음질", "MP3 bitrate", Numbers("kbps", 128, 192, 256, 320), 3);
        var aac = p.Combo<int>("AAC 음질", "AAC bitrate", Numbers("kbps", 96, 128, 192, 256), 3);
        var depth = p.Combo<int>("WAV·AIFF 비트 심도", "WAV/AIFF bit depth", Numbers("bit", 16, 24, 32), 0);
        var rate = p.Combo<int>("샘플레이트", "Sample rate",
            [new("원본 유지", "Original", 0), new("44100 Hz", "44100 Hz", 44100), new("48000 Hz", "48000 Hz", 48000)], 0);
        return (p, () =>
        {
            int m = mp3(), a = aac(), w = depth(), s = rate();
            return (i, o, opt, pr) => AudioConversionService.Convert(i, o, opt.ToFormat,
                mp3Bitrate: m, aacBitrate: a, wavBitDepth: w, sampleRate: s, progress: pr);
        });
    }

    public static (FrameworkElement, Func<ConvertFile>) Video()
    {
        var p = new SettingsPanel();
        var crf = p.Segments<int>("화질", "Quality",
            [new("낮음", "Low", 28), new("보통", "Medium", 23), new("높음", "High", 18)], 1,
            "MP4·MKV·WEBM에 적용 (CRF 28 / 23 / 18). 높을수록 파일이 커집니다", "MP4, MKV and WEBM (CRF 28 / 23 / 18). Higher means bigger files");
        var preset = p.Combo<string>("인코딩 속도", "Encoding speed",
            [new("빠르게", "Fast", "fast"), new("보통", "Medium", "medium"), new("느리게 (더 작은 파일)", "Slow (smaller file)", "slow")], 1);
        var audio = p.Combo<int>("오디오 음질", "Audio bitrate", Numbers("kbps", 96, 128, 192, 256, 320), 2);
        var res = p.Combo<string>("해상도", "Resolution",
            [new("원본 유지", "Original", ""), new("360p", "360p", "360"), new("480p", "480p", "480"),
             new("720p", "720p", "720"), new("1080p", "1080p", "1080"), new("4K (2160p)", "4K (2160p)", "2160")], 0);
        return (p, () =>
        {
            int c = crf(), a = audio();
            string pr = preset(), r = res();
            return (i, o, opt, prog) => VideoConversionService.Convert(i, o, opt.ToFormat,
                crf: c, preset: pr, audioBitrate: a, resolution: r, progress: prog);
        });
    }
}
