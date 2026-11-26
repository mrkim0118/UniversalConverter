<p align="center">
  <img src="UniversalConverter/Assets/AppIcon.png" width="128" alt="UniversalConverter icon">
</p>

<h1 align="center">UniversalConverter</h1>

<p align="center">A Windows desktop app that converts image, audio, video, document and font files in one place.</p>

<p align="center">Author: <b>Manggdy</b></p>

<p align="center"><b>English</b> | <a href="README.ko.md">한국어</a></p>

Drop files on the home screen, pick the target format, and convert them all at once.

## Download

Get `UniversalConverter_Setup_v1.1.0.exe` from the [Releases](https://github.com/mrkim0118/UniversalConverter/releases/latest) page and run it.
If the required .NET 10 Desktop Runtime is missing, the installer downloads and installs it for you.

## Features

- **Five converters in one app**: Image, audio, video, document and font.
- **Drag and drop**: Drop files on the home screen and the right converter opens.
- **Batch conversion**: Keep adding files, even mixed formats such as JPG and PNG, and convert them all at once.
- **Simple format picker**: Pick **From → To** formats in two dropdowns and press a single **Convert** button.
- **Output folder**: Next to the source file by default, or any folder you choose. Existing files are never overwritten; new ones are saved as `name (1).jpg`.
- **Image quality as a number (1-100)**: Including JPG → JPG re-saving.
- **Windows 11 (Fluent) design**: Light and dark mode that follow your system setting.
- **English and Korean**: The UI starts in your Windows display language.
- **One-click FFmpeg install**: For audio and video conversion.
- **Update check**: On launch, checks the latest GitHub release and asks whether to download and install the new version.

## Requirements

- Windows 10 1809 or later, 64-bit
- .NET 10 Desktop Runtime (installed automatically by the setup program)
- FFmpeg for audio and video; the first audio or video conversion offers to install it

## Usage

### Supported formats

| Converter | Formats |
|---|---|
| Image | In: HEIC, JPG, PDF, PSD, WEBP, PNG, GIF, TIFF, BMP, SVG, AVIF, ICO<br>Out: JPG, PNG, WEBP, GIF, TIFF, BMP, AVIF, ICO, PDF, PSD, HEIC |
| Audio | MP3, FLAC, WAV, AIFF, AAC (any to any) |
| Video | MP4, AVI, WEBM, MKV, FLV, MPEG (any to any) |
| Document | PDF, DOCX, HTML, TXT, RTF → TXT, HTML, PDF, DOCX (selected pairs)<br>CSV, JSON, XML, XLS between each other |
| Font | TTF, OTF, WOFF, WOFF2, EOT (any to any) |

CSV files saved by Korean Excel (CP949) are read without broken characters.

## Data & Settings

| Data | Location |
|---|---|
| FFmpeg | `%LocalAppData%\UniversalConverter\ffmpeg` |

## Build from Source

You need the .NET 10 SDK and, to build the installer, [Inno Setup 6](https://jrsoftware.org/isdl.php).

### Build

```powershell
dotnet build Converters.sln
```

### Test

```powershell
dotnet test Tests/ConverterTests
```

### Release

```powershell
UniversalConverter\build_release.bat
```

`build_release.bat` publishes the app to `UniversalConverter/publish/win-x64` and, when Inno Setup is installed, writes the installer to `UniversalConverter/installer`.

## Project Layout

| Path | Contents |
|---|---|
| `UniversalConverter/` | The all-in-one app (WPF, .NET 10) |
| `UniversalConverter/Views/` | Home screen, shared converter screen (ConverterView), settings panels |
| `UniversalConverter/Assets/` | App icon |
| `Shared/` | Conversion logic and models shared by every app |
| `Tests/ConverterTests/` | Unit tests (CSV, encodings, PDF wrapping, FFmpeg progress, UI loading) |
| `ImageConverter/`, `AudioConverter/`, … | Standalone single-purpose converters (.NET 8) |
| `setup/` | Shared installer script (.NET runtime install) |
| `UniversalConverter/Services/GitHubUpdater.cs` | GitHub release update check |

## Libraries

- [Magick.NET](https://github.com/dlemstra/Magick.NET): images
- [FFmpeg](https://ffmpeg.org/): audio and video (downloaded at runtime from [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds))
- [NPOI](https://github.com/nissl-lab/npoi), [PdfPig](https://github.com/UglyToad/PdfPig), [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore), [HtmlAgilityPack](https://html-agility-pack.net/): documents
