namespace UniversalConverter.Services;

public static class AudioConversionService
{
    /// <param name="mp3Bitrate">MP3 비트레이트 (kbps): 128·192·256·320</param>
    /// <param name="aacBitrate">AAC 비트레이트 (kbps): 96·128·192·256</param>
    /// <param name="wavBitDepth">WAV/AIFF 비트심도: 16·24·32</param>
    /// <param name="sampleRate">샘플레이트 (Hz): 0=원본유지·44100·48000</param>
    public static void Convert(string inputPath, string outputPath, string toFormat,
        int mp3Bitrate = 320, int aacBitrate = 256, int wavBitDepth = 16, int sampleRate = 0,
        IProgress<double>? progress = null)
    {
        var srArg = sampleRate > 0 ? $"-ar {sampleRate} " : "";

        var pcmCodec = wavBitDepth switch
        {
            24 => "pcm_s24le",
            32 => "pcm_s32le",
            _  => "pcm_s16le",
        };
        var pcmCodecBe = wavBitDepth switch
        {
            24 => "pcm_s24be",
            32 => "pcm_s32be",
            _  => "pcm_s16be",
        };

        var codecArgs = toFormat.ToUpper() switch
        {
            "MP3"  => $"-codec:a libmp3lame -b:a {mp3Bitrate}k -q:a 0",
            "FLAC" => "-codec:a flac",
            "WAV"  => $"-codec:a {pcmCodec}",
            "AIFF" => $"-codec:a {pcmCodecBe} -f aiff",
            "AAC"  => $"-codec:a aac -b:a {aacBitrate}k",
            _ => throw new NotSupportedException($"지원하지 않는 오디오 형식: {toFormat}")
        };

        var totalSeconds = progress != null ? FFmpegHelper.GetDurationSeconds(inputPath) : 0;
        FFmpegHelper.Run($"-i \"{inputPath}\" -vn {srArg}{codecArgs} -y \"{outputPath}\"", totalSeconds, progress);
    }
}
