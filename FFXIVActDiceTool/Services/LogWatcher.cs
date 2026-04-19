using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FFXIVActDiceTool.Services;

public class LogWatcher : IDisposable
{
    private readonly TimeSpan _tailInterval = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _scanInterval = TimeSpan.FromSeconds(2);

    private CancellationTokenSource? _cts;
    private Task? _watchTask;
    private string? _sourcePath;
    private string? _activeFile;

    public event EventHandler<string>? LineReceived;
    public event EventHandler<string>? StatusChanged;

    public bool IsWatching { get; private set; }
    public string? ActiveFilePath => _activeFile;

    public void Start(string sourcePath)
    {
        Stop();

        _sourcePath = sourcePath;
        _cts = new CancellationTokenSource();
        _watchTask = Task.Run(() => WatchLoopAsync(_cts.Token));
        IsWatching = true;
        StatusChanged?.Invoke(this, "감시 시작");
    }

    public void Stop()
    {
        if (!IsWatching)
        {
            return;
        }

        _cts?.Cancel();
        try
        {
            _watchTask?.Wait(1500);
        }
        catch
        {
            // ignore
        }

        _cts?.Dispose();
        _cts = null;
        _watchTask = null;
        IsWatching = false;
        StatusChanged?.Invoke(this, "감시 중지");
    }

    private async Task WatchLoopAsync(CancellationToken token)
    {
        var source = _sourcePath ?? string.Empty;
        var isDirectory = Directory.Exists(source);
        var rolloverFolder = isDirectory ? source : Path.GetDirectoryName(source);
        var rolloverPattern = isDirectory ? "*.log" : TryGetRolloverSearchPattern(source);
        var canRollover = !string.IsNullOrWhiteSpace(rolloverFolder) && !string.IsNullOrWhiteSpace(rolloverPattern);

        if (!isDirectory && !File.Exists(source))
        {
            StatusChanged?.Invoke(this, "로그 경로를 찾을 수 없습니다.");
            IsWatching = false;
            return;
        }

        _activeFile = isDirectory
            ? ResolveLatestLogFile(source, "*.log")
            : source;

        if (string.IsNullOrWhiteSpace(_activeFile))
        {
            StatusChanged?.Invoke(this, "활성 로그 파일이 없습니다.");
            return;
        }

        StatusChanged?.Invoke(this, $"연결됨: {_activeFile}");

        long position = new FileInfo(_activeFile).Length;
        var lastScan = DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            if (_activeFile is null)
            {
                await Task.Delay(_tailInterval, token);
                continue;
            }

            position = await ReadNewLinesAsync(_activeFile, position, token);

            if (canRollover && DateTime.UtcNow - lastScan >= _scanInterval)
            {
                lastScan = DateTime.UtcNow;
                var newest = ResolveLatestLogFile(rolloverFolder!, rolloverPattern!);

                if (!string.IsNullOrWhiteSpace(newest) && !string.Equals(newest, _activeFile, StringComparison.OrdinalIgnoreCase))
                {
                    position = await ReadNewLinesAsync(_activeFile, position, token);
                    _activeFile = newest;
                    StatusChanged?.Invoke(this, $"로그 전환: {_activeFile}");
                    position = 0; // 새 파일은 처음부터 읽음
                }
            }

            await Task.Delay(_tailInterval, token);
        }
    }

    private async Task<long> ReadNewLinesAsync(string filePath, long startPosition, CancellationToken token)
    {
        const int retryDelayMs = 300;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (startPosition > fs.Length)
                {
                    startPosition = 0;
                }

                fs.Seek(startPosition, SeekOrigin.Begin);
                using var reader = new StreamReader(fs, Encoding.UTF8, true, 4096, leaveOpen: true);

                while (!reader.EndOfStream)
                {
                    token.ThrowIfCancellationRequested();
                    var line = await reader.ReadLineAsync(token);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        LineReceived?.Invoke(this, line);
                    }
                }

                return fs.Position;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"읽기 재시도: {ex.Message}");
                await Task.Delay(retryDelayMs, token);
            }
        }

        return startPosition;
    }

    private static string? ResolveLatestLogFile(string folder, string pattern)
    {
        return Directory.EnumerateFiles(folder, pattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()
            ?.FullName;
    }

    private static string? TryGetRolloverSearchPattern(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var prefix = Regex.Replace(fileName, @"([_-]?\d{8}([_-]?\d{6})?)$", string.Empty);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        return $"{prefix}*{extension}";
    }

    public void Dispose()
    {
        Stop();
    }
}
