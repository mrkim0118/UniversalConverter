namespace UniversalConverter.Services;

public static class VideoConversionService
{
    /// <param name="crf">H.264/VP9 품질 (0-51, 낮을수록 고화질, 기본 23)</param>
    /// <param name="preset">H.264 인코딩 속도 (ultrafast/fast/medium/slow/veryslow)</param>
    /// <param name="audioBitrate">오디오 비트레이트 (kbps): 96·128·192·256·320</param>
    /// <param name="resolution">출력 해상도: "" = 원본, "2160"·"1080"·"720"·"480"·"360"</param>
    public static void Convert(string inputPath, string outputPath, string toFormat,
        int crf = 23, string preset = "medium", int audioBitrate = 192, string resolution = "",
        IProgress<double>? progress = null)
    {
        var scaleFilter = string.IsNullOrEmpty(resolution)
            ? ""
            : $"-vf scale=-2:{resolution} ";

        var codecArgs = toFormat.ToUpper() switch
        {
            "MP4"  => $"-codec:v libx264 -preset {preset} -crf {crf} {scaleFilter}-codec:a aac -b:a {audioBitrate}k -movflags +faststart",
            "AVI"  => $"-codec:v mpeg4 -q:v 5 {scaleFilter}-codec:a mp3 -b:a {audioBitrate}k",
            "WEBM" => $"-codec:v libvpx-vp9 -crf {crf} -b:v 0 {scaleFilter}-codec:a libopus -b:a {audioBitrate}k",
            "MKV"  => $"-codec:v libx264 -preset {preset} -crf {crf} {scaleFilter}-codec:a aac -b:a {audioBitrate}k",
            "FLV"  => $"-codec:v flv1 -q:v 5 {scaleFilter}-codec:a mp3 -b:a {audioBitrate}k -ar 44100",
            "MPEG" => $"-codec:v mpeg2video -b:v 5000k {scaleFilter}-codec:a mp2 -b:a {audioBitrate}k",
            _ => throw new NotSupportedException($"지원하지 않는 비디오 형식: {toFormat}")
        };

        var totalSeconds = progress != null ? FFmpegHelper.GetDurationSeconds(inputPath) : 0;
        FFmpegHelper.Run($"-i \"{inputPath}\" {codecArgs} -y \"{outputPath}\"", totalSeconds, progress);
    }
}
