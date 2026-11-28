using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using UniversalConverter.Models;
using UniversalConverter.Services;

namespace UniversalConverter.Views;

/// <summary>Converts one file: (input path, output path, option, per-file progress 0-100).</summary>
public delegate void ConvertFile(string input, string output, ConversionOption option, IProgress<double> progress);

/// <summary>Shared screen for all five converters; only the settings panel and the convert call differ.</summary>
public partial class ConverterView : UserControl
{
    private readonly (string Ko, string En) _title;
    private readonly List<ConversionOption> _all;
    private readonly Func<ConvertFile> _prepare;
    private readonly bool _needsFFmpeg;
    private readonly Dictionary<string, string> _formatByExt = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _files = [];
    private readonly ComboBoxItem _autoFrom = new() { Tag = "" };
    private string? _outDir;   // null = next to each source file

    public event Action? BackRequested;

    public ConverterView((string Ko, string En) title, Color accent, List<ConversionOption> options,
        FrameworkElement? settings, Func<ConvertFile> prepare, bool needsFFmpeg = false)
    {
        InitializeComponent();
        _title = title;
        _all = options;
        _prepare = prepare;
        _needsFFmpeg = needsFFmpeg;
        Resources["AccentBrush"] = new SolidColorBrush(accent);
        Resources["AccentTintBrush"] = new SolidColorBrush(Color.FromArgb(0x26, accent.R, accent.G, accent.B));   // translucent: works in light and dark

        foreach (var o in _all)
            foreach (var ext in Extensions(o.FileFilter))
                _formatByExt.TryAdd(ext, o.FromFormat);

        HeaderFormats.Text = string.Join(" · ", _all.Select(o => o.FromFormat).Distinct());
        foreach (var f in _all.Select(o => o.FromFormat).Distinct().Order())
            FromCombo.Items.Add(new ComboBoxItem { Tag = f, Content = f });
        FromCombo.SelectedIndex = 0;

        if (settings == null) SettingsBox.Visibility = Visibility.Collapsed;
        else
        {
            SettingsHost.Content = settings;
            NotifySettings();   // the first From/To selection happened before the panel was attached
        }

        if (_needsFFmpeg)
        {
            CheckFFmpeg();
            // Another screen may have installed FFmpeg meanwhile; skip while this one is installing (button disabled).
            IsVisibleChanged += (_, _) =>
            {
                if (IsVisible && FFmpegBanner.Visibility == Visibility.Visible && FFmpegActionBtn.IsEnabled) CheckFFmpeg();
            };
        }

        LanguageService.LanguageChanged += ApplyLanguage;
        ApplyLanguage(LanguageService.IsKorean);
    }

    private static bool Ko => LanguageService.IsKorean;

    // "JPG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg" -> ".jpg", ".jpeg"
    private static IEnumerable<string> Extensions(string fileFilter) =>
        fileFilter.Split('|')[1].Split(';').Select(p => p.Trim().TrimStart('*'));

    public bool CanHandle(string path) => _formatByExt.ContainsKey(Path.GetExtension(path));

    private void ApplyLanguage(bool ko)
    {
        HeaderTitle.Text = ko ? _title.Ko : _title.En;
        BackBtn.Content = ko ? "← 홈" : "← Home";
        LangBtn.Content = ko ? "EN" : "한국어";
        AutomationProperties.SetName(LangBtn, ko ? "Switch to English" : "한국어로 바꾸기");
        FromLabel.Text = ko ? "원본 형식" : "From";
        ToLabel.Text = ko ? "변환할 형식" : "To";
        ConvertBtn.Content = ko ? "변환" : "Convert";
        ClearBtn.Content = ko ? "목록 비우기" : "Clear";
        FilesEmpty.Text = ko ? "아직 고른 파일이 없습니다. 위에 끌어다 놓거나, 바로 '변환'을 누르면 파일 선택 창이 열립니다."
                             : "No files yet. Drop them above, or press Convert to choose files.";
        RefreshFiles();
        UpdateOutDirText();
        if (_needsFFmpeg) CheckFFmpeg();
        else if (ConversionProgress.Visibility != Visibility.Visible) SetIdleStatus();
    }

    private void SetIdleStatus() =>
        StatusText.Text = Ko ? "파일과 변환할 형식을 고른 뒤 '변환'을 누르세요" : "Choose files and a target format, then press Convert";

    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

    private void LangBtn_Click(object sender, RoutedEventArgs e) => LanguageService.Toggle();

    // ── From / To ──
    private string FormatOf(string path) => _formatByExt[Path.GetExtension(path)];

    private List<string> LoadedFormats() => _files.Select(FormatOf).Distinct().Order().ToList();

    /// <summary>Target formats that every one of the source formats can be converted to.</summary>
    internal static List<string> CommonTargets(IEnumerable<ConversionOption> all, IList<string> sources) =>
        all.Where(o => o.FromFormat == sources[0]).Select(o => o.ToFormat)
           .Where(t => sources.All(s => all.Any(o => o.FromFormat == s && o.ToFormat == t))).ToList();

    // Offer only targets that every source format in play can become; keep the previous target when still valid.
    private void To_Changed(object sender, SelectionChangedEventArgs e) => NotifySettings();

    // Lets the settings panel disable options that don't apply to the current source/target formats.
    private void NotifySettings()
    {
        if (SettingsHost?.Content is not SettingsPanel { FormatsChanged: { } notify }) return;
        if (FromCombo.SelectedItem is not ComboBoxItem { Tag: string from }
            || ToCombo.SelectedItem is not ComboBoxItem { Tag: string to }) return;
        notify(from == "" ? LoadedFormats() : [from], to);
    }

    private void From_Changed(object? sender, SelectionChangedEventArgs? e)
    {
        if (ToCombo == null || FromCombo.SelectedItem is not ComboBoxItem { Tag: string from }) return;   // fires during InitializeComponent
        var sources = from == "" ? LoadedFormats() : [from];
        if (sources.Count == 0) return;
        var targets = CommonTargets(_all, sources);
        var previous = (ToCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        ToCombo.Items.Clear();
        foreach (var t in targets) ToCombo.Items.Add(new ComboBoxItem { Tag = t, Content = t });
        ToCombo.SelectedItem = ToCombo.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == previous)
                               ?? ToCombo.Items.Cast<ComboBoxItem>().FirstOrDefault();
    }

    // ── Files: drop or pick (they accumulate) ──
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
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) LoadFiles(files);
    }

    private void PickFiles_Click(object sender, RoutedEventArgs e)
    {
        var patterns = string.Join(";", _formatByExt.Keys.Select(x => "*" + x));
        var dlg = new OpenFileDialog
        {
            Title = Ko ? "변환할 파일 선택 (여러 개 선택 가능)" : "Select files to convert (multiple allowed)",
            Filter = Ko ? $"지원하는 파일|{patterns}|모든 파일 (*.*)|*.*" : $"Supported files|{patterns}|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog() == true) LoadFiles(dlg.FileNames);
    }

    /// <summary>Adds the supported files (any mix of this converter's formats) to the list.</summary>
    public void LoadFiles(IEnumerable<string> files)
    {
        var all = files.ToArray();
        var ok = all.Where(f => File.Exists(f) && CanHandle(f)).ToArray();
        var added = ok.Where(f => !_files.Contains(f, StringComparer.OrdinalIgnoreCase)).ToArray();
        _files.AddRange(added);
        RefreshFiles();
        var skipped = all.Length - ok.Length;
        StatusText.Text = Ko
            ? $"파일 {added.Length}개를 추가했습니다." + (skipped > 0 ? $" 지원하지 않는 파일 {skipped}개는 뺐습니다." : "")
            : $"Added {added.Length} file(s)." + (skipped > 0 ? $" Skipped {skipped} unsupported file(s)." : "");
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        RefreshFiles();
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string path }) _files.Remove(path);
        RefreshFiles();
    }

    public record FileRow(string Name, string Format, string Path);

    // With files loaded, the source is "their formats" and the From box is locked to that.
    private void RefreshFiles()
    {
        DropText.Text = Ko ? "파일을 여기에 끌어다 놓거나 클릭해서 추가하세요 (여러 개 가능)" : "Drop files here or click to add (multiple allowed)";
        AutomationProperties.SetName(DropZone, DropText.Text);
        FilesList.ItemsSource = _files.Select(f => new FileRow(Path.GetFileName(f), FormatOf(f), f)).ToList();
        FilesTitle.Text = Ko ? $"변환할 파일 ({_files.Count})" : $"Files to convert ({_files.Count})";
        FilesEmpty.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearBtn.Visibility = _files.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        var formats = LoadedFormats();
        if (formats.Count > 0)
        {
            _autoFrom.Content = (Ko ? "불러온 파일: " : "Loaded: ") + string.Join(", ", formats);
            FromCombo.IsEnabled = false;
            if (!FromCombo.Items.Contains(_autoFrom)) FromCombo.Items.Insert(0, _autoFrom);
            if (FromCombo.SelectedItem == _autoFrom) From_Changed(null, null);
            else FromCombo.SelectedItem = _autoFrom;
        }
        else if (FromCombo.Items.Contains(_autoFrom))
        {
            FromCombo.Items.Remove(_autoFrom);
            FromCombo.IsEnabled = true;
            FromCombo.SelectedIndex = 0;
        }
    }

    // ── Output folder ──
    private void OutDir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = Ko ? "변환된 파일을 저장할 폴더 선택" : "Select output folder" };
        if (dlg.ShowDialog() != true) return;
        _outDir = dlg.FolderName;
        UpdateOutDirText();
    }

    private void OutDirReset_Click(object sender, RoutedEventArgs e)
    {
        _outDir = null;
        UpdateOutDirText();
    }

    private void UpdateOutDirText()
    {
        var where = _outDir == null ? (Ko ? "원본과 같은 폴더" : "same folder as source") : Path.GetFileName(_outDir.TrimEnd('\\'));
        OutDirBtn.Content = (Ko ? "저장 위치: " : "Save to: ") + where + "  ✎";
        OutDirBtn.ToolTip = _outDir;
        OutDirResetBtn.Visibility = _outDir == null ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(OutDirResetBtn, Ko ? "원본과 같은 폴더로 되돌리기" : "Reset to source folder");
    }

    // ── FFmpeg ──
    private void CheckFFmpeg()
    {
        if (FFmpegHelper.FindFFmpeg() != null)
        {
            FFmpegBanner.Visibility = Visibility.Collapsed;
            if (ConversionProgress.Visibility != Visibility.Visible) SetIdleStatus();
            return;
        }
        FFmpegBanner.Visibility = Visibility.Visible;
        FFmpegBanner.SetResourceReference(Border.BackgroundProperty, "SystemFillColorCautionBackgroundBrush");
        FFmpegStatusText.Text = Ko
            ? "⚠  이 변환에는 FFmpeg가 필요합니다. 오른쪽 버튼을 누르면 자동으로 설치합니다."
            : "⚠  FFmpeg is required for this conversion. Use the button to install it automatically.";
        FFmpegActionBtn.Content = Ko ? "FFmpeg 설치" : "Install FFmpeg";
        FFmpegActionBtn.Visibility = Visibility.Visible;
        StatusText.Text = Ko ? "FFmpeg 설치가 필요합니다" : "FFmpeg installation required";
    }

    private async void FFmpegInstall_Click(object sender, RoutedEventArgs e)
    {
        var ko = Ko;
        FFmpegActionBtn.IsEnabled = false;
        FFmpegActionBtn.Content = ko ? "설치 중..." : "Installing...";
        FFmpegBanner.SetResourceReference(Border.BackgroundProperty, "SystemFillColorSuccessBackgroundBrush");
        var progress = new Progress<string>(msg =>
        {
            FFmpegStatusText.Text = $"⏳  {msg}";
            StatusText.Text = msg;
        });

        try
        {
            await FFmpegHelper.DownloadFFmpegAsync(progress);
            FFmpegStatusText.Text = ko ? "✅  FFmpeg 설치 완료! 이제 변환할 수 있습니다." : "✅  FFmpeg installed. You can convert now.";
            FFmpegActionBtn.Visibility = Visibility.Collapsed;
            SetIdleStatus();
        }
        catch (Exception ex)
        {
            FFmpegBanner.SetResourceReference(Border.BackgroundProperty, "SystemFillColorCriticalBackgroundBrush");
            FFmpegStatusText.Text = ko ? $"❌  설치 실패: {ex.Message}" : $"❌  Installation failed: {ex.Message}";
            FFmpegActionBtn.Content = ko ? "다시 시도" : "Retry";
            FFmpegActionBtn.IsEnabled = true;
        }
    }

    // ── Convert ──
    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        var ko = Ko;
        if ((ToCombo.SelectedItem as ComboBoxItem)?.Tag is not string target)
        {
            StatusText.Text = ko ? "불러온 파일들이 공통으로 바꿀 수 있는 형식이 없습니다. 형식이 다른 파일을 빼 주세요."
                                 : "These files share no common target format. Remove the odd ones out.";
            return;
        }

        ConvertFile convert;
        try { convert = _prepare(); }   // read settings on the UI thread; throws on invalid input
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, ko ? "설정 확인" : "Check settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_needsFFmpeg && FFmpegHelper.FindFFmpeg() == null)
        {
            MessageBox.Show(
                ko ? "FFmpeg가 설치되어 있지 않습니다.\n상단의 'FFmpeg 설치' 버튼을 눌러주세요."
                   : "FFmpeg is not installed.\nUse the 'Install FFmpeg' button at the top.",
                ko ? "FFmpeg 필요" : "FFmpeg required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Each loaded file converts with the option for its own format; with no files, ask for files of the chosen source.
        (string File, ConversionOption Option)[] jobs;
        if (_files.Count > 0)
            jobs = _files.Select(f => (f, _all.First(o => o.FromFormat == FormatOf(f) && o.ToFormat == target))).ToArray();
        else
        {
            var picked = _all.First(o => o.FromFormat == (string)((ComboBoxItem)FromCombo.SelectedItem).Tag && o.ToFormat == target);
            var openDlg = new OpenFileDialog
            {
                Title = ko ? $"{picked.FromFormat} 파일 선택 (여러 개 선택 가능)" : $"Select {picked.FromFormat} files (multiple allowed)",
                Filter = ko ? $"{picked.FileFilter}|모든 파일 (*.*)|*.*" : $"{picked.FileFilter}|All files (*.*)|*.*",
                Multiselect = true,
            };
            if (openDlg.ShowDialog() != true || openDlg.FileNames.Length == 0) return;
            jobs = openDlg.FileNames.Select(f => (f, picked)).ToArray();
        }

        var title = $"→ {target}";
        var outDir = _outDir;
        var total = jobs.Length;
        var success = 0;
        var errors = new List<string>();
        var failed = new List<string>();
        string? lastOutDir = null;

        StatusText.Text = ko ? $"[{title}] 변환 중... 0 / {total}" : $"[{title}] Converting... 0 / {total}";
        ConversionProgress.Value = 0;
        ConversionProgress.Visibility = Visibility.Visible;
        IsEnabled = false;

        await Task.Run(() =>
        {
            foreach (var (file, option) in jobs)
            {
                var name = Path.GetFileName(file);
                var fileIndex = success + errors.Count;
                var fileProgress = new Progress<double>(pct => Dispatcher.Invoke(() =>
                {
                    ConversionProgress.Value = (fileIndex + pct / 100.0) / total * 100.0;
                    StatusText.Text = ko
                        ? $"[{title}] 변환 중... {fileIndex + 1} / {total}  ({(int)pct}%)"
                        : $"[{title}] Converting... {fileIndex + 1} / {total}  ({(int)pct}%)";
                }));

                string? outPath = null;
                try
                {
                    var dir = outDir ?? Path.GetDirectoryName(file)!;
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    outPath = Path.Combine(dir, baseName + option.OutputExtension);
                    for (var counter = 1; File.Exists(outPath); counter++)
                        outPath = Path.Combine(dir, $"{baseName} ({counter}){option.OutputExtension}");
                    convert(file, outPath, option, fileProgress);
                    lastOutDir = dir;
                    success++;
                }
                catch (Exception ex)
                {
                    if (outPath != null) try { File.Delete(outPath); } catch { } // drop the half-written output
                    errors.Add($"  • {name}: {ex.Message}");
                    failed.Add(file);
                }

                var current = success + errors.Count;
                Dispatcher.Invoke(() =>
                {
                    ConversionProgress.Value = (double)current / total * 100;
                    StatusText.Text = ko ? $"[{title}] 변환 중... {current} / {total}"
                                         : $"[{title}] Converting... {current} / {total}";
                });
            }
        });

        ConversionProgress.Visibility = Visibility.Collapsed;
        IsEnabled = true;
        _files.Clear();
        _files.AddRange(failed.Where(f => File.Exists(f) && CanHandle(f)));   // keep failures for a retry
        RefreshFiles();

        if (errors.Count > 0)
        {
            StatusText.Text = ko ? $"완료: {success}개 성공, {errors.Count}개 실패" : $"Done: {success} succeeded, {errors.Count} failed";
            MessageBox.Show(
                $"[{title}]\n\n✅ {(ko ? "성공" : "Succeeded")}: {success} / ❌ {(ko ? "실패" : "Failed")}: {errors.Count}\n\n" +
                string.Join("\n", errors),
                ko ? "변환 결과" : "Conversion result", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            StatusText.Text = ko ? $"✅ {success}개 파일 변환 완료 ({title})" : $"✅ {success} file(s) converted ({title})";
            if (MessageBox.Show(
                    ko ? $"[{title}]\n\n{success}개 파일을 변환했습니다.\n\n저장 위치: {lastOutDir}\n\n저장 폴더를 열까요?"
                       : $"[{title}]\n\n{success} file(s) converted.\n\nSaved to: {lastOutDir}\n\nOpen the folder?",
                    ko ? "변환 완료" : "Conversion complete",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes && lastOutDir != null)
                Process.Start("explorer.exe", lastOutDir);
        }
    }
}
