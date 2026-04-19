# FFXIV ACT Dice Tool (WPF MVP)

## 프로젝트 구조

- `FFXIVActDiceTool/App.xaml` : 앱 엔트리
- `FFXIVActDiceTool/Views/MainWindow.xaml` : 메인 UI
- `FFXIVActDiceTool/ViewModels/MainViewModel.cs` : 화면 상태/명령 처리
- `FFXIVActDiceTool/Services/LogWatcher.cs` : 로그 tail + 롤오버 전환
- `FFXIVActDiceTool/Services/DiceParser.cs` : /dice 파싱 (패턴 체인)
- `FFXIVActDiceTool/Services/DiceSessionManager.cs` : 세션 집계/통계
- `FFXIVActDiceTool/Services/RankCalculator.cs` : N순위 계산
- `FFXIVActDiceTool/Models/*` : 도메인 모델
- `FFXIVActDiceTool/Helpers/*` : MVVM 보조(ObservableObject, RelayCommand, Converter)

## 실행 흐름

1. 파일/폴더 경로를 선택 후 감시 시작
2. `LogWatcher`가 새 줄만 읽어 이벤트 발생
3. `DiceParser`가 여러 패턴 순차 시도 후 `DiceRollEntry` 반환
4. 세션 Running 상태에서만 `DiceSessionManager`에 누적
5. UI는 `ObservableCollection`과 속성 바인딩으로 즉시 갱신
6. 집계 종료 시 최고/최저/총 굴림/참여 인원 계산
7. 순위 조회 시 `RankCalculator`가 DenseRank 기준으로 결과 제공

## 참고

- 로그 파일은 `FileShare.ReadWrite`로 열립니다.
- 폴더 지정 시 주기적 재탐색 후 최신 `.log` 파일로 자동 전환됩니다.
- 파일 전환 중복을 줄이기 위해 최근 처리 키 캐시를 사용합니다.
