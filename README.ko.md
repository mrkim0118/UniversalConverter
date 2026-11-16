<p align="center">
  <img src="UniversalConverter/Assets/AppIcon.png" width="96" alt="UniversalConverter 아이콘">
</p>

<h1 align="center">UniversalConverter</h1>

<p align="center">
  이미지 · 오디오 · 비디오 · 문서 · 폰트 파일을 한 프로그램에서 변환하는 Windows 데스크톱 앱<br>
  만든 사람: <b>Manggdy</b>
</p>

<p align="center">
  <a href="README.md">English</a> | <b>한국어</b>
</p>

---

## 내려받기

[Releases](https://github.com/mrkim0118/UniversalConverter/releases/latest) 페이지에서 `UniversalConverter_Setup_v1.1.0.exe`를 받아 실행하세요.
필요한 .NET 10 Desktop Runtime이 없으면 설치 프로그램이 자동으로 받아 설치합니다.

## 주요 기능

- **변환기 다섯 개를 한곳에서**: 이미지, 오디오, 비디오, 문서, 폰트
- **끌어다 놓기**: 홈 화면에 파일을 놓으면 형식에 맞는 변환기가 바로 열립니다.
- **여러 파일 한 번에 변환**: 파일을 계속 추가할 수 있고, JPG와 PNG처럼 형식이 섞여 있어도 한 번에 바꿉니다.
- **원본 형식 → 변환할 형식**을 드롭다운으로 고르고 **변환** 버튼 하나로 실행합니다.
- **저장 위치**: 기본은 원본과 같은 폴더이며, 다른 폴더를 고를 수도 있습니다. 같은 이름의 파일은 덮어쓰지 않고 `이름 (1).jpg`처럼 저장합니다.
- **이미지 품질 직접 입력(1~100)**, JPG → JPG 다시 저장 지원
- **Windows 11 스타일(Fluent) 디자인**, 시스템 설정을 따르는 라이트/다크 모드
- **한국어 / 영어** 화면 (Windows 표시 언어에 맞춰 시작)
- **FFmpeg 자동 설치**: 오디오·비디오 변환에 필요한 FFmpeg를 앱 안에서 받아 설치합니다.

## 지원 형식

| 변환기 | 형식 |
|---|---|
| 이미지 | 입력: HEIC, JPG, PDF, PSD, WEBP, PNG, GIF, TIFF, BMP, SVG, AVIF, ICO<br>출력: JPG, PNG, WEBP, GIF, TIFF, BMP, AVIF, ICO, PDF, PSD, HEIC |
| 오디오 | MP3, FLAC, WAV, AIFF, AAC (서로 변환) |
| 비디오 | MP4, AVI, WEBM, MKV, FLV, MPEG (서로 변환) |
| 문서 | PDF, DOCX, HTML, TXT, RTF → TXT·HTML·PDF·DOCX 일부<br>CSV, JSON, XML, XLS 사이 변환 |
| 폰트 | TTF, OTF, WOFF, WOFF2, EOT (서로 변환) |

한국어 엑셀에서 저장한 CSV(CP949)도 한글이 깨지지 않게 읽습니다.

## 실행 환경

- Windows 10 1809 이상, 64비트
- .NET 10 Desktop Runtime (설치 프로그램이 자동으로 설치)
- 오디오·비디오 변환을 처음 할 때 FFmpeg 설치 안내가 나옵니다. FFmpeg는 `%LocalAppData%\UniversalConverter\ffmpeg`에 저장됩니다.

## 직접 빌드하기

필요한 것: .NET 10 SDK, (설치 파일을 만들 때) [Inno Setup 6](https://jrsoftware.org/isdl.php)

```bash
dotnet build Converters.sln
```

```bash
dotnet test Tests/ConverterTests
```

```bash
UniversalConverter\build_release.bat
```

`build_release.bat`는 `UniversalConverter/publish/win-x64`에 실행 파일을 만들고, Inno Setup이 있으면 `UniversalConverter/installer`에 설치 파일을 만듭니다.

## 폴더 구조

```
UniversalConverter/      통합 변환기 앱 (WPF, .NET 10)
  Views/                 홈 화면, 공용 변환 화면(ConverterView), 설정 패널
  Assets/                앱 아이콘
Shared/                  모든 앱이 함께 쓰는 변환 로직과 모델
Tests/ConverterTests/    단위 테스트 (CSV, 인코딩, PDF 줄바꿈, FFmpeg 진행률, 화면 로딩 등)
ImageConverter/ …        변환기별 단독 앱 (.NET 8)
setup/                   설치 프로그램 공용 스크립트 (.NET 런타임 설치)
```

## 사용한 라이브러리

- [Magick.NET](https://github.com/dlemstra/Magick.NET): 이미지 변환
- [FFmpeg](https://ffmpeg.org/): 오디오·비디오 변환 (실행 중에 [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds)에서 내려받음)
- [NPOI](https://github.com/nissl-lab/npoi), [PdfPig](https://github.com/UglyToad/PdfPig), [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore), [HtmlAgilityPack](https://html-agility-pack.net/): 문서 변환

## 만든 사람

**Manggdy**
