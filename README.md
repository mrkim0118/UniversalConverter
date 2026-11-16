<p align="center">
  <img src="UniversalConverter/Assets/AppIcon.png" width="96" alt="UniversalConverter icon">
</p>

<h1 align="center">UniversalConverter</h1>

<p align="center">
  A Windows desktop app that converts image, audio, video, document and font files in one place<br>
  Made by <b>Manggdy</b>
</p>

<p align="center">
  <b>English</b> | <a href="README.ko.md">한국어</a>
</p>

---

## Download

Get `UniversalConverter_Setup_v1.1.0.exe` from the [Releases](https://github.com/mrkim0118/UniversalConverter/releases/latest) page and run it.
If the required .NET 10 Desktop Runtime is missing, the installer downloads and installs it for you.

## Features

- **Five converters in one app**: image, audio, video, document and font
- **Drag and drop**: drop files on the home screen and the right converter opens
- **Batch conversion**: keep adding files, even mixed formats such as JPG and PNG, and convert them all at once
- Pick **From → To** formats in two dropdowns and press a single **Convert** button
- **Output folder**: next to the source file by default, or any folder you choose. Existing files are never overwritten; new ones are saved as `name (1).jpg`
- **Image quality as a number (1-100)**, including JPG → JPG re-saving
- **Windows 11 (Fluent) design** with light and dark mode that follow your system setting
- **English and Korean** UI, starting in your Windows display language
- **One-click FFmpeg install** for audio and video conversion

## Supported formats

| Converter | Formats |
|---|---|
| Image | In: HEIC, JPG, PDF, PSD, WEBP, PNG, GIF, TIFF, BMP, SVG, AVIF, ICO<br>Out: JPG, PNG, WEBP, GIF, TIFF, BMP, AVIF, ICO, PDF, PSD, HEIC |
| Audio | MP3, FLAC, WAV, AIFF, AAC (any to any) |
| Video | MP4, AVI, WEBM, MKV, FLV, MPEG (any to any) |
| Document | PDF, DOCX, HTML, TXT, RTF → TXT, HTML, PDF, DOCX (selected pairs)<br>CSV, JSON, XML, XLS between each other |
| Font | TTF, OTF, WOFF, WOFF2, EOT (any to any) |

CSV files saved by Korean Excel (CP949) are read without broken characters.

## Requirements

- Windows 10 1809 or later, 64-bit
- .NET 10 Desktop Runtime (installed automatically by the setup program)
- The first audio or video conversion offers to install FFmpeg. It is stored in `%LocalAppData%\UniversalConverter\ffmpeg`.

## Building from source

You need the .NET 10 SDK and, to build the installer, [Inno Setup 6](https://jrsoftware.org/isdl.php).

```bash
dotnet build Converters.sln
```

```bash
dotnet test Tests/ConverterTests
```

```bash
UniversalConverter\build_release.bat
```

`build_release.bat` publishes the app to `UniversalConverter/publish/win-x64` and, when Inno Setup is installed, writes the installer to `UniversalConverter/installer`.

## Project layout

```
UniversalConverter/      The all-in-one app (WPF, .NET 10)
  Views/                 Home screen, shared converter screen (ConverterView), settings panels
  Assets/                App icon
Shared/                  Conversion logic and models shared by every app
Tests/ConverterTests/    Unit tests (CSV, encodings, PDF wrapping, FFmpeg progress, UI loading)
ImageConverter/ …        Standalone single-purpose converters (.NET 8)
setup/                   Shared installer script (.NET runtime install)
```

## Libraries

- [Magick.NET](https://github.com/dlemstra/Magick.NET): images
- [FFmpeg](https://ffmpeg.org/): audio and video (downloaded at runtime from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds))
- [NPOI](https://github.com/nissl-lab/npoi), [PdfPig](https://github.com/UglyToad/PdfPig), [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore), [HtmlAgilityPack](https://html-agility-pack.net/): documents

## Author

**Manggdy**
