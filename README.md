# FFXIV ACT Dice Tool

한국 FFXIV `/dice` 결과를 실시간 집계하는 **ACT OverlayPlugin Web Overlay**입니다. DLL, .NET, 별도 서버 또는 빌드가 필요하지 않습니다.

## 설치

1. ACT, FFXIV Parsing Plugin, OverlayPlugin을 실행합니다.
2. OverlayPlugin에서 **New → Custom Overlay**를 만듭니다.
3. URL에 아래 주소를 입력합니다.

   **https://bogbu.github.io/FFXIV-ACT-Dice-Tool/**

4. Overlay를 활성화하고 원하는 크기로 조절합니다.

> Overlay에 `ACT 연결됨 · 로그 대기 중`이 표시되면 연결된 상태입니다. 일반 브라우저에서는 `브라우저 미리보기`로 표시되며 UI는 정상 동작하지만 ACT 로그는 수신하지 않습니다.

## 기능과 사용법

- **집계 시작**: 이전 목록·통계·순위 결과·중복 캐시를 비우고 새 세션을 시작합니다.
- **집계 종료**: 로그 수집을 멈추고 종료 시각과 현재 결과를 유지합니다. OverlayPlugin 구독은 계속 유지됩니다.
- **초기화**: 세션과 화면 데이터를 대기 상태로 되돌립니다.
- **실시간 결과**: 참여 인원, 총 굴림, 최고/최저 값과 모든 동점 플레이어를 즉시 표시합니다.
- **순위 조회**: 높은 순 또는 낮은 순의 N위를 DenseRank로 조회합니다. `900, 900, 700`은 900 두 명이 1위, 700이 2위입니다.
- **목록 제한**: 화면에는 최근 2,000행만 유지하지만 세션 통계와 순위는 전체 roll을 사용합니다.
- **중복 방지**: timestamp, 플레이어, 값, 원본 로그 조합의 최근 500개 key를 보관합니다.

세션이 `집계 중`일 때만 감지한 주사위를 추가합니다. 지원 범위에는 한국어 bracket timestamp, ACT localized pipe 형식과 기존 영문 bracket/pipe/simple 로그 형식이 포함되며 값 범위는 기존과 동일한 `0..999`입니다.

## OverlayPlugin 요구사항

이 Overlay는 OverlayPlugin Web API의 `LogLine` 이벤트를 구독합니다. 이벤트의 원본 `rawLine`을 우선 사용하고, 없는 버전에서는 문서화된 `line` field 배열을 pipe 형식으로 결합합니다. OverlayPlugin 전역 API가 없는 경우 자동으로 browser preview mode가 됩니다.

데이터는 메모리에만 있으며 새로고침하면 초기화됩니다. 계정, 서버, 데이터베이스, localStorage 복구 기능은 사용하지 않습니다.

## 브라우저 개발 및 테스트

정적 파일이므로 별도 build step은 없습니다. ES module 보안 정책 때문에 로컬 파일을 직접 여는 대신 저장소 루트에서 정적 HTTP 서버를 실행하십시오.

```bash
python3 -m http.server 8000
```

`http://localhost:8000/?debug=1`을 열면 일반 UI 아래에 가짜 로그 입력기가 표시됩니다. 자동 회귀 테스트는 Node.js 20 이상에서 실행합니다.

```bash
npm test
```

## 구조

```text
OverlayPlugin LogLine
  → src/overlay/overlayBridge.js
  → src/parser/diceParser.js
  → src/services/duplicateFilter.js
  → src/services/diceSessionManager.js
  → src/services/rankCalculator.js
  → src/ui/diceUI.js
```

`app.js`는 각 계층을 연결하기만 하며, DOM에는 사용자 문자열을 `textContent`로 추가합니다. 지속 timer나 전체 화면 polling을 사용하지 않고 dice 로그가 파싱된 경우에만 UI를 갱신합니다.

## GitHub Pages

`.github/workflows/deploy-pages.yml`은 `main` push 시 테스트 후 저장소의 정적 파일을 GitHub Pages 공식 Actions로 배포합니다. Repository **Settings → Pages → Source**가 **GitHub Actions**로 설정되어 있어야 합니다.

CSS/JavaScript는 project site에서도 동작하도록 상대 경로를 사용하며, asset query version `2.0.0`으로 OverlayPlugin WebView 캐시를 갱신할 수 있습니다.

## 마이그레이션 기능 비교

- [x] 한국 서버 `/dice` 및 기존 raw ACT 형식 파싱
- [x] 실시간 목록 / 집계 시작 / 집계 종료 / 초기화
- [x] 최고값 / 최저값 / 최고·최저 동점
- [x] 총 굴림 수 / 대소문자를 무시한 고유 참여 인원
- [x] 높은 순 N위 / 낮은 순 N위 / DenseRank
- [x] bounded 중복 로그 방지 / 최대 UI 2,000행
- [x] OverlayPlugin `LogLine` 및 일반 브라우저 fallback
- [x] responsive Web UI / GitHub Pages 자동 배포

기존 ACT Plugin DLL과 WinForms/.NET 프로젝트는 Web Overlay 이식 및 회귀 테스트 완료 후 제거되었습니다.
