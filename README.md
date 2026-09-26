# DK Randomize AI Image Prompt Generator

AI 이미지 생성에 사용할 프롬프트를 **저장하고, 조합하고, 랜덤 선택해서 복사하기 위한 Windows 데스크톱 프로그램**입니다.

이미지를 직접 생성하는 프로그램은 아닙니다. 자주 사용하는 캐릭터·작가/스타일·추가 프롬프트를 정리해 두고 필요한 조합을 빠르게 만드는 데 초점을 맞춥니다.

[![Release](https://img.shields.io/github/v/release/danhk0612/DK_Randomize_AI_Image_Prompt_Generator)](https://github.com/danhk0612/DK_Randomize_AI_Image_Prompt_Generator/releases/latest)
[![Build](https://github.com/danhk0612/DK_Randomize_AI_Image_Prompt_Generator/actions/workflows/build.yml/badge.svg)](https://github.com/danhk0612/DK_Randomize_AI_Image_Prompt_Generator/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 다운로드

- **[최신 정식 릴리스](https://github.com/danhk0612/DK_Randomize_AI_Image_Prompt_Generator/releases/latest)**
- **[Windows x64 ZIP 바로 받기](https://github.com/danhk0612/DK_Randomize_AI_Image_Prompt_Generator/releases/latest/download/DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64.zip)**

ZIP을 원하는 폴더에 압축 해제한 뒤 아래 파일을 실행합니다.

```text
DKRandomizeAIImagePromptGenerator.exe
```

배포본에는 다음 두 파일이 들어 있습니다.

```text
DKRandomizeAIImagePromptGenerator.exe       ← 실행용 런처
DKRandomizeAIImagePromptGenerator.App.exe   ← 실제 프로그램
```

두 파일은 같은 폴더에 두는 것을 권장합니다.

## 필요한 환경

- Windows 10 version 1809 이상 또는 Windows 11
- x64 Windows
- **Microsoft .NET 10 Desktop Runtime x64**

.NET 10 Desktop Runtime이 설치되어 있지 않으면 런처가 이를 감지하고 Microsoft 공식 다운로드 페이지를 열 수 있도록 안내합니다. .NET 런타임 자체는 배포 ZIP에 포함하지 않습니다.

## 주요 기능

- **프롬프트 라이브러리**
  - Character / Artist · Style / Additional 분류
  - Positive / Negative 프롬프트 분리 저장
  - 제목, 태그, 메모, 대표 이미지 저장
  - 검색, 부분 태그 필터, 태그 클릭 필터
  - 갤러리 / 목록 보기
  - 수정일·생성일·제목 정렬
  - 생성, 수정, 복제, 삭제
  - 규격화된 TXT 파일/폴더 일괄 가져오기
  - 동일 파일명의 대표 이미지 자동 연결

- **Mixer**
  - 카테고리별 Direct / Random / Disabled
  - 여러 프롬프트 동시 선택
  - 선택 순서 변경 및 드래그 정렬
  - 카테고리별 랜덤 선택 개수 지정
  - 전체 랜덤 또는 카테고리별 다시 뽑기
  - Positive / Negative 결과 직접 편집
  - Positive / Negative 개별 복사

- **최근 기록**
  - 최종 편집된 결과 저장
  - 선택 순서, 모드, 랜덤 개수까지 함께 저장
  - 20개 단위 페이징
  - 이전 조합을 Mixer로 복원
  - 개별 삭제 / 전체 기록 삭제

- **설정과 데이터 관리**
  - System / Light / Dark 테마
  - 마지막 창 크기·위치·최대화 상태 기억
  - LocalAppData 또는 실행 파일 폴더(Portable) 저장 방식 선택
  - ZIP 백업 / 복원
  - GitHub Releases 기반 업데이트 확인 및 설치

## TXT 프롬프트 일괄 가져오기

Prompt Library의 **가져오기** 버튼에서 폴더 하나 또는 여러 TXT 파일을 선택할 수 있습니다. 현재 선택한 Character / Artist · Style / Additional 분류로 등록됩니다.

TXT 파일명은 프롬프트 제목이 됩니다. 파일은 UTF-8 형식을 사용하며 아래 섹션을 지원합니다.

```text
[Positive]
1girl, long hair, blue eyes

[Negative]
low quality, blurry

[Tags]
character, sample

[Memo]
메모 내용
```

가져오기 규칙:

- 제목은 TXT 파일명에서 자동 지정됩니다.
- `[Positive]` 또는 `[Negative]` 중 하나에는 반드시 내용이 있어야 합니다.
- `[Tags]`와 `[Memo]`는 없어도 됩니다.
- 같은 분류에 같은 제목이 이미 있을 때 처리 방식을 선택할 수 있습니다.
  - **기본**: 해당 파일을 실패 처리하고 기존 데이터는 변경하지 않습니다.
  - **강제 병합**: 기존 항목을 가져온 파일 내용으로 덮어씁니다.
  - **이름 변경**: 기존 항목은 유지하고 `제목 (2)`, `제목 (3)`처럼 사용 가능한 번호를 붙여 새 항목으로 추가합니다.
- TXT와 같은 폴더에 같은 이름의 `.png`, `.webp`, `.jpg`, `.jpeg`, `.bmp`가 있으면 대표 이미지로 자동 등록합니다.
- 이미지가 없어도 정상적으로 가져옵니다.
- 여러 파일 중 하나가 실패해도 나머지 파일은 계속 처리됩니다.

예:

```text
stellive_rin.txt
stellive_rin.png
stellive_lize.txt
stellive_lize.webp
```

## 빠른 사용법

1. **Prompt Library**에서 자주 사용하는 프롬프트를 등록합니다.
2. **Mixer**에서 각 카테고리를 Direct, Random, Disabled 중 하나로 설정합니다.
3. Direct에서는 원하는 프롬프트를 직접 고르고 순서를 정합니다.
4. Random에서는 필요한 개수를 지정하고 랜덤 선택합니다.
5. 생성된 Positive / Negative 결과를 필요하면 직접 수정합니다.
6. 각각 복사해서 사용하는 이미지 생성 도구에 붙여 넣습니다.
7. 나중에 다시 쓸 조합은 **History**에 저장합니다.

## 데이터 저장 위치

기본값은 Windows 사용자별 LocalAppData입니다.

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\
│  └─ prompts.db
├─ images\
├─ backups\
└─ settings.json
```

- 프롬프트와 최근 기록: SQLite
- 대표 이미지: `images\`
- 앱 설정: `settings.json`
- 수동 백업: ZIP

대표 이미지는 앱 관리 폴더로 복사되며 원본 이미지는 수정하지 않습니다.

### Portable 모드

**설정 → 데이터 저장 위치 → 실행 파일 폴더 (Portable)** 를 선택하면 다음 실행부터 프로그램 폴더에 데이터를 저장합니다.

Portable 모드는 실행 파일 옆의 `portable.mode` 파일로 구분합니다.

저장 위치를 변경해도 기존 데이터는 자동 이동하지 않습니다. 기존 데이터를 함께 옮기려면 **백업 → 저장 위치 변경 → 재실행 → 복원** 순서를 권장합니다.

## 업데이트

**설정 → 앱 업데이트 → 업데이트 확인**에서 GitHub Releases의 새 버전을 확인할 수 있습니다.

새 버전이 있으면:

1. 업데이트 ZIP 다운로드
2. 현재 프로그램 종료
3. 프로그램 파일 교체
4. 자동 재실행

순서로 진행합니다.

LocalAppData의 사용자 데이터와 Portable 모드의 `data`, `images`, `backups`, `settings.json`, `portable.mode`는 업데이트 대상에서 제외됩니다.

## 백업과 복원

설정 화면에서 앱 데이터를 ZIP으로 백업하고 복원할 수 있습니다.

백업에는 다음이 포함됩니다.

- 프롬프트 데이터베이스
- 대표 이미지
- 설정
- 최근 기록

PC 이동이나 Portable/LocalAppData 저장 위치 전환 전에 백업을 만들어 두는 것을 권장합니다.

## 문제 해결

### 실행했는데 .NET Runtime 안내가 나오는 경우

Microsoft .NET 10 Desktop Runtime x64가 필요합니다. 런처의 안내에 따라 Microsoft 공식 다운로드 페이지에서 Desktop Runtime을 설치한 뒤 다시 실행합니다.

### 실제 프로그램 파일을 찾을 수 없다고 나오는 경우

`DKRandomizeAIImagePromptGenerator.exe`와 `DKRandomizeAIImagePromptGenerator.App.exe`가 같은 폴더에 있는지 확인합니다. ZIP의 두 파일을 함께 압축 해제해야 합니다.

### 업데이트가 실패하는 경우

프로그램이 설치된 폴더에 쓰기 권한이 있는지 확인합니다. 일반 사용자 권한으로 수정 가능한 폴더에 압축 해제해서 사용하는 것을 권장합니다.

## 개발

기술 구성:

- C# / .NET 10
- WPF / XAML
- SQLite (`Microsoft.Data.Sqlite`)
- Windows x64
- 네이티브 C++ 런처

프로젝트 구조:

```text
src/
├─ DKRandomizeAIImagePromptGenerator.Core/
├─ DKRandomizeAIImagePromptGenerator.Wpf/
└─ DKRandomizeAIImagePromptGenerator.Launcher/

tests/
└─ DKRandomizeAIImagePromptGenerator.Tests/
```

로컬 검증:

```powershell
.\scripts\verify-release.ps1 -Launch
```

상세 설계와 개발 문서는 `docs/`를 참고하세요. 변경 내역은 [CHANGELOG.md](CHANGELOG.md)에 정리되어 있습니다.

## 라이선스

이 프로젝트는 [MIT License](LICENSE)로 배포됩니다.
