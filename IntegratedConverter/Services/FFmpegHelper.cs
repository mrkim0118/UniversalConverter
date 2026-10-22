using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace UniversalConverter.Services;

/// <summary>
/// FFmpeg 바이너리 탐색, 다운로드, 실행을 담당하는 공유 헬퍼
/// </summary>
public static class FFmpegHelper
{
    private static string? _ffmpegPath;

    public static string? FindFFmpeg()
    {
        if (_ffmpegPath != null) return _ffmpegPath;

        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "ffmpeg.exe"),
            Path.Combine(appDir, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(appDir, "ffmpeg", "bin", "ffmpeg.exe"),
        };
        foreach (var p in candidates)
            if (File.Exists(p)) return _ffmpegPath = p;

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            proc?.WaitForExit(3000);
            if (proc?.ExitCode == 0)
                return _ffmpegPath = "ffmpeg";
        }
        catch { }

        return null;
    }

    public static async Task DownloadFFmpegAsync(IProgress<string>? progress = null)
    {
        var isKorean = LanguageService.IsKorean;
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var ffmpegDir = Path.Combine(appDir, "ffmpeg");
        Directory.CreateDirectory(ffmpegDir);

        const string url =
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        progress?.Report(isKorean ? "FFmpeg 다운로드 중..." : "Downloading FFmpeg...");

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromMinutes(5);

        var zipPath = Path.Combine(ffmpegDir, "ffmpeg.zip");
        await using (var stream = await http.GetStreamAsync(url))
        await using (var fs = File.Create(zipPath))
        {
            await stream.CopyToAsync(fs);
        }

        progress?.Report(isKorean ? "압축 해제 중..." : "Extracting files...");

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                {
                    entry.ExtractToFile(Path.Combine(ffmpegDir, "ffmpeg.exe"), overwrite: true);
                    break;
                }
            }
        }

        File.Delete(zipPath);
        _ffmpegPath = Path.Combine(ffmpegDir, "ffmpeg.exe");

        progress?.Report(isKorean ? "FFmpeg 설치 완료!" : "FFmpeg installation complete!");
    }

    /// <summary>
    /// 미디어 파일의 총 재생 시간(초)을 반환합니다. 실패 시 0을 반환합니다.
    /// </summary>
    public static double GetDurationSeconds(string inputPath)
    {
        var ffmpeg = _ffmpegPath ?? FindFFmpeg();
        if (ffmpeg == null) return 0;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = $"-i \"{inputPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(); // -i 단독 실행은 항상 exit code != 0 → 무시

        var m = Regex.Match(stderr, @"Duration:\s*(\d+):(\d+):(\d+\.?\d*)");
        if (!m.Success) return 0;

        var h = int.Parse(m.Groups[1].Value);
        var min = int.Parse(m.Groups[2].Value);
        var sec = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        return h * 3600 + min * 60 + sec;
    }

    /// <summary>
    /// FFmpeg CLI 를 실행하고 오류 시 예외를 던집니다.
    /// progress가 제공되면 stderr의 time= 필드를 파싱해 0~100 진행률을 보고합니다.
    /// </summary>
    public static void Run(string arguments, double totalSeconds = 0, IProgress<double>? progress = null)
    {
        var ffmpeg = _ffmpegPath ?? FindFFmpeg()
            ?? throw new InvalidOperationException(
                LanguageService.IsKorean
                    ? "FFmpeg를 찾을 수 없습니다.\n변환기 페이지에서 FFmpeg를 설치해주세요."
                    : "FFmpeg could not be found.\nPlease install it from the converter page.");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var errorLines = new System.Collections.Generic.List<string>();
        string? line;
        while ((line = process.StandardError.ReadLine()) != null)
        {
            errorLines.Add(line);

            if (progress != null && totalSeconds > 0)
            {
                var idx = line.IndexOf("time=", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var timeStr = line.Substring(idx + 5).Split(' ')[0]; // "HH:MM:SS.xx"
                    var parts = timeStr.Split(':');
                    if (parts.Length == 3
                        && int.TryParse(parts[0], out var h)
                        && int.TryParse(parts[1], out var mn)
                        && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                    {
                        var elapsed = h * 3600 + mn * 60 + s;
                        var pct = Math.Min(elapsed / totalSeconds * 100.0, 99.0);
                        progress.Report(pct);
                    }
                }
            }
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var errorLine = errorLines.LastOrDefault(l => l.Contains("Error") || l.Contains("error"))
                            ?? errorLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))
                            ?? (LanguageService.IsKorean ? "알 수 없는 오류" : "Unknown error");
            throw new Exception(
                LanguageService.IsKorean
                    ? $"FFmpeg 변환 실패: {errorLine.Trim()}"
                    : $"FFmpeg conversion failed: {errorLine.Trim()}");
        }
    }
}
