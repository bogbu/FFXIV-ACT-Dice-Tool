# FFXIV ACT Dice Tool

한국 FFXIV `/dice` 로그를 Advanced Combat Tracker(ACT) 안에서 실시간 집계하는 **.NET Framework 4.8 WinForms 플러그인**입니다. 별도 실행 파일, 로그 경로 선택, 파일 tail 감시는 사용하지 않습니다.

## 필요 프로그램

- Windows 및 [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)
- Advanced Combat Tracker(ACT)
- ACT가 FFXIV 로그를 읽도록 구성하는 **FFXIV Parsing Plugin** (FFXIV 로그 수신에 필요)
- Visual Studio 2022 또는 .NET Framework 4.8을 빌드할 수 있는 MSBuild

## 빌드

프로젝트는 ACT 설치 경로를 저장소에 하드코딩하지 않습니다. 다음 중 하나를 사용하십시오.

```powershell
# 환경 변수
$env:ACT_PATH = 'C:\Program Files (x86)\Advanced Combat Tracker'
msbuild .\FFXIVActDiceTool\FFXIVActDiceTool.csproj /t:Restore,Build /p:Configuration=Release

# 또는 MSBuild 속성
msbuild .\FFXIVActDiceTool\FFXIVActDiceTool.csproj /t:Restore,Build /p:Configuration=Release /p:ACTPath="C:\Program Files (x86)\Advanced Combat Tracker"
```

`ACTPath`는 `Advanced Combat Tracker.exe`가 들어 있는 **폴더**입니다. 대안으로 저장소 루트의 `lib/Advanced Combat Tracker.exe`도 인식하지만, ACT 바이너리는 Git에 커밋하지 마십시오. 결과물은 `FFXIVActDiceTool/bin/Release/net48/FFXIVActDiceTool.dll`입니다.

코어 회귀 테스트는 ACT 설치 없이 실행할 수 있습니다. 테스트 프로젝트는 ACT 비의존 소스를 링크해 파서, 세션, 순위, 중복 필터의 격리를 검증합니다.

```powershell
dotnet test .\FFXIVActDiceTool.Tests\FFXIVActDiceTool.Tests.csproj
```

## ACT에 설치

1. ACT와 FFXIV Parsing Plugin을 설치하고 게임 로그가 ACT에 표시되는지 확인합니다.
2. ACT의 **Plugins** 탭에서 **Browse**를 누릅니다.
3. 빌드된 `FFXIVActDiceTool.dll`을 선택해 **Add/Enable Plugin** 합니다.
4. 플러그인 탭과 `FFXIV Dice Tool 활성화됨` 상태를 확인합니다.

ACT가 전달한 실시간 `OnLogLineRead`만 처리하며 import 로그는 기본적으로 무시합니다. 플러그인을 비활성화하면 이벤트 구독을 해제하므로 재활성화해도 중복 구독되지 않습니다.

## 사용법

- **집계 시작**: 새 세션과 중복 캐시, 목록, 결과를 초기화하고 이후 주사위를 수집합니다. ACT 로그 연결은 플러그인 활성화 동안 계속 유지됩니다.
- **집계 종료**: 종료 시각을 기록하고 최고/최저(동점 모두), 참여 인원, 총 굴림을 확정합니다.
- **초기화**: 세션, UI 목록, 통계, 순위 결과, 중복 캐시를 비웁니다. ACT 이벤트 연결은 유지합니다.
- **순위 조회**: 높은 순/낮은 순과 N을 선택합니다. 기본 DenseRank이므로 `900, 900, 700`은 각각 1위 두 명과 2위 한 명입니다.
- 실시간 표는 성능을 위해 최근 2,000행만 보이지만 세션 데이터는 별도로 모두 유지됩니다.

## 구조

```text
ACT OnLogLineRead
  -> ACT/ActLogSource (import 필터, 구독 수명주기)
  -> Plugin/DicePluginController (입력 조정)
  -> Services/DiceParser
  -> Services/DiceDuplicateFilter
  -> Services/DiceSessionManager / RankCalculator
  -> Plugin/DicePluginControl (WinForms UI, UI thread marshal)
```

- `Plugin/DicePlugin.cs`: `IActPluginV1` 엔트리 및 ACT 탭 수명주기
- `Logging/ILogSource.cs`, `ACT/ActLogSource.cs`: ACT 종속성을 입력 경계로 제한
- `Models/*`, `Services/*`: ACT/UI 비의존 도메인 로직
- `Plugin/DicePluginControl.cs`: 기본 WinForms 컨트롤만 사용하는 UI
- `FFXIVActDiceTool.Tests`: ACT 없이 실행하는 코어 회귀 테스트

## Legacy

이 저장소는 기존 WPF 독립 실행 프로그램에서 ACT 플러그인으로 전환되었습니다. 로그 파일/폴더 선택, 직접 파일 감시, 롤오버 감시 및 별도 EXE는 플러그인에서 의미가 없어 제거되었습니다.
