using System.Collections.ObjectModel;
using System.Windows;
using Forms = System.Windows.Forms;
using FFXIVActDiceTool.Helpers;
using FFXIVActDiceTool.Models;
using FFXIVActDiceTool.Services;
using Microsoft.Win32;

namespace FFXIVActDiceTool.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly LogWatcher _logWatcher = new();
    private readonly DiceParser _parser = new();
    private readonly DiceSessionManager _sessionManager = new();
    private readonly RankCalculator _rankCalculator = new();
    private readonly Queue<string> _recentKeys = new();
    private readonly HashSet<string> _recentSet = new();

    private const int DedupMaxSize = 500;

    private string _logPath = string.Empty;
    private string _watchStatus = "대기";
    private string _connectionStatus = "미연결";
    private int _rankInput = 1;
    private RankDirection _selectedDirection = RankDirection.Highest;
    private string _rankResultText = "조회 전";
    private string _highestSummary = "-";
    private string _lowestSummary = "-";
    private int _totalRollCount;
    private int _uniquePlayerCount;
    private DateTime? _sessionStart;
    private DateTime? _sessionEnd;

    public MainViewModel()
    {
        Rolls = new ObservableCollection<DiceRollEntry>();

        SelectFileCommand = new RelayCommand(SelectFile);
        SelectFolderCommand = new RelayCommand(SelectFolder);
        StartWatchingCommand = new RelayCommand(StartWatching, () => !string.IsNullOrWhiteSpace(LogPath));
        StopWatchingCommand = new RelayCommand(StopWatching, () => _logWatcher.IsWatching);
        StartSessionCommand = new RelayCommand(StartSession, () => !_sessionManager.CurrentSession.IsRunning);
        EndSessionCommand = new RelayCommand(EndSession, () => _sessionManager.CurrentSession.IsRunning);
        ResetSessionCommand = new RelayCommand(ResetSession);
        QueryRankCommand = new RelayCommand(QueryRank);

        _logWatcher.LineReceived += OnLineReceived;
        _logWatcher.StatusChanged += (_, message) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                WatchStatus = message;
                ConnectionStatus = _logWatcher.ActiveFilePath ?? "미연결";
                RefreshCommands();
            });
        };
    }

    public ObservableCollection<DiceRollEntry> Rolls { get; }

    public string LogPath
    {
        get => _logPath;
        set
        {
            if (SetProperty(ref _logPath, value))
            {
                RefreshCommands();
            }
        }
    }

    public string WatchStatus { get => _watchStatus; set => SetProperty(ref _watchStatus, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
    public int RankInput { get => _rankInput; set => SetProperty(ref _rankInput, value); }
    public RankDirection SelectedDirection { get => _selectedDirection; set => SetProperty(ref _selectedDirection, value); }
    public string RankResultText { get => _rankResultText; set => SetProperty(ref _rankResultText, value); }
    public string HighestSummary { get => _highestSummary; set => SetProperty(ref _highestSummary, value); }
    public string LowestSummary { get => _lowestSummary; set => SetProperty(ref _lowestSummary, value); }
    public int TotalRollCount { get => _totalRollCount; set => SetProperty(ref _totalRollCount, value); }
    public int UniquePlayerCount { get => _uniquePlayerCount; set => SetProperty(ref _uniquePlayerCount, value); }
    public DateTime? SessionStart { get => _sessionStart; set => SetProperty(ref _sessionStart, value); }
    public DateTime? SessionEnd { get => _sessionEnd; set => SetProperty(ref _sessionEnd, value); }

    public RelayCommand SelectFileCommand { get; }
    public RelayCommand SelectFolderCommand { get; }
    public RelayCommand StartWatchingCommand { get; }
    public RelayCommand StopWatchingCommand { get; }
    public RelayCommand StartSessionCommand { get; }
    public RelayCommand EndSessionCommand { get; }
    public RelayCommand ResetSessionCommand { get; }
    public RelayCommand QueryRankCommand { get; }

    private void SelectFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Log files (*.log)|*.log|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            LogPath = dialog.FileName;
        }
    }

    private void SelectFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "ACT 로그 폴더 선택",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            LogPath = dialog.SelectedPath;
        }
    }

    private void StartWatching()
    {
        _logWatcher.Start(LogPath);
        RefreshCommands();
    }

    private void StopWatching()
    {
        _logWatcher.Stop();
        ConnectionStatus = "미연결";
        RefreshCommands();
    }

    private void StartSession()
    {
        if (!EnsureWatchingForSessionStart())
        {
            return;
        }

        _sessionManager.StartSession();
        SessionStart = _sessionManager.CurrentSession.StartTime;
        SessionEnd = null;
        HighestSummary = "-";
        LowestSummary = "-";
        TotalRollCount = 0;
        UniquePlayerCount = 0;
        RankResultText = "조회 전";
        Rolls.Clear();
        ClearDedupCache();
        RefreshCommands();
    }

    private bool EnsureWatchingForSessionStart()
    {
        if (_logWatcher.IsWatching)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(LogPath))
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "집계를 시작하려면 ACT 로그 폴더를 선택하세요.",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() != Forms.DialogResult.OK)
            {
                WatchStatus = "집계 시작 취소";
                return false;
            }

            LogPath = dialog.SelectedPath;
        }

        _logWatcher.Start(LogPath);
        return _logWatcher.IsWatching;
    }

    private void EndSession()
    {
        var result = _sessionManager.StopSession();
        SessionEnd = _sessionManager.CurrentSession.EndTime;
        HighestSummary = FormatRollSummary(result.HighestRolls);
        LowestSummary = FormatRollSummary(result.LowestRolls);
        TotalRollCount = result.TotalRollCount;
        UniquePlayerCount = result.UniquePlayerCount;
        RefreshCommands();
    }

    private void ResetSession()
    {
        _sessionManager.ResetSession();
        Rolls.Clear();
        SessionStart = null;
        SessionEnd = null;
        HighestSummary = "-";
        LowestSummary = "-";
        TotalRollCount = 0;
        UniquePlayerCount = 0;
        RankResultText = "조회 전";
        ClearDedupCache();
        RefreshCommands();
    }

    private void QueryRank()
    {
        var query = _rankCalculator.QueryRank(
            _sessionManager.CurrentSession.Rolls,
            RankInput,
            SelectedDirection,
            RankMode.DenseRank);

        if (!query.Exists)
        {
            RankResultText = "결과 없음";
            return;
        }

        var players = string.Join(", ", query.Entries.Select(x => $"{x.PlayerName}({x.RollValue})"));
        RankResultText = $"{query.RequestedRank}위 / 값 {query.RollValue}: {players}";
    }

    private void OnLineReceived(object? sender, string line)
    {
        var parsed = _parser.Parse(line);
        if (parsed is null)
        {
            return;
        }

        if (IsDuplicate(parsed))
        {
            return;
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_sessionManager.CurrentSession.IsRunning)
            {
                _sessionManager.AddRoll(parsed);
                Rolls.Add(parsed);

                if (Rolls.Count > 2000)
                {
                    Rolls.RemoveAt(0);
                }

                var result = _sessionManager.CalculateResult();
                TotalRollCount = result.TotalRollCount;
                UniquePlayerCount = result.UniquePlayerCount;
                HighestSummary = FormatRollSummary(result.HighestRolls);
                LowestSummary = FormatRollSummary(result.LowestRolls);
            }
        });
    }

    private bool IsDuplicate(DiceRollEntry entry)
    {
        var key = $"{entry.Timestamp:O}|{entry.PlayerName}|{entry.RollValue}|{entry.RawLogLine}";
        if (_recentSet.Contains(key))
        {
            return true;
        }

        _recentSet.Add(key);
        _recentKeys.Enqueue(key);

        while (_recentKeys.Count > DedupMaxSize)
        {
            var old = _recentKeys.Dequeue();
            _recentSet.Remove(old);
        }

        return false;
    }

    private void ClearDedupCache()
    {
        _recentKeys.Clear();
        _recentSet.Clear();
    }

    private static string FormatRollSummary(List<DiceRollEntry> rolls)
    {
        if (rolls.Count == 0)
        {
            return "-";
        }

        var value = rolls.First().RollValue;
        var players = string.Join(", ", rolls.Select(x => x.PlayerName));
        return $"{value} ({players})";
    }

    private void RefreshCommands()
    {
        StartWatchingCommand.RaiseCanExecuteChanged();
        StopWatchingCommand.RaiseCanExecuteChanged();
        StartSessionCommand.RaiseCanExecuteChanged();
        EndSessionCommand.RaiseCanExecuteChanged();
    }
}
