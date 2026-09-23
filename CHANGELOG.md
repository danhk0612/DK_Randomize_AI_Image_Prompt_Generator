# Changelog

## Unreleased

### Added

- Prompt Library bulk import from a selected folder or multiple UTF-8 TXT files
- `[Positive]`, `[Negative]`, `[Tags]`, and `[Memo]` import sections
- automatic same-basename representative image matching
- per-file import progress, success/failure results, and duplicate-title protection

## v1.0.0 — 2026-09-19

첫 정식 릴리스.

### 주요 기능

- Character / Artist · Style / Additional 프롬프트 라이브러리
- Positive / Negative 프롬프트 분리 저장
- Direct / Random / Disabled 조합 모드
- 카테고리별 다중 선택, 순서 변경, 랜덤 개수 지정
- 검색, 태그 필터, 갤러리/목록 보기, 대표 이미지
- 조합 결과 직접 편집 및 개별 복사
- 최근 기록 저장, 페이징, Mixer 복원, 전체 기록 삭제
- System / Light / Dark 테마
- 창 크기·위치·최대화 상태 기억
- LocalAppData / 실행 파일 폴더(Portable) 데이터 저장 방식 선택
- ZIP 백업 및 복원
- GitHub Releases 기반 업데이트 확인 및 자동 교체/재실행
- .NET 10 Desktop Runtime을 포함하지 않는 경량 2파일 배포

### 배포 구성

```text
DKRandomizeAIImagePromptGenerator.exe
DKRandomizeAIImagePromptGenerator.App.exe
```

첫 번째 파일은 .NET 10 Desktop Runtime x64를 확인하는 네이티브 런처이며,
두 번째 파일은 실제 WPF 애플리케이션입니다.
