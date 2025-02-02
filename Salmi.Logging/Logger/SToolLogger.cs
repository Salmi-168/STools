using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Pastel;
using Salmi.Logging.Helper;
using Salmi.Logging.Logger.Pipes;

namespace Salmi.Logging.Logger;

internal class SToolLogger : ILogger, IDisposable
{
    public string Name { get; }

    private readonly SToolLoggerSettings _settings;
    private readonly Task? _fileTask;
    private readonly Task? _pipeTask;

    public SToolLogger(string name, SToolLoggerSettings settings)
    {
        Name = name;
        _settings = settings;

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.File))
            _fileTask = Task.Run(() => ProcessLogQueue(settings.LogFilePath));

        if (OperatingSystem.IsWindows() && _settings.SToolLoggerType.HasFlag(SToolLoggerType.Pipes) && _settings.HasPipeConfig())
            _pipeTask = Task.Run(() => ProcessPipeQueue(settings.PipeSettings));
    }

    private static readonly ConcurrentQueue<string> FileQueue = [];
    private static readonly ConcurrentQueue<string> PipeQueue = [];

    private static readonly AutoResetEvent FileQueueSignal = new(initialState: false);
    private static readonly AutoResetEvent PipeQueueSignal = new(initialState: false);

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        string logMessage = $"[{DateTime.Now:HH:mm:ss HH:mm:ss}]";
        string name = $"[{Name}]".SetLength(30);
        string unFormatedMessage = logMessage + " " + $"[{logLevel}]".SetLength(13) + $" {name} - {formatter(state, exception)}";
        string consoleMessage = logMessage + " " + $"[{logLevel.ToString().SetLength(11).ToColorCodedConsoleString(logLevel)}]" + $" {name} - {formatter(state, exception).Pastel(logLevel.ToColor())}";

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.Console))
        {
            Console.WriteLine(consoleMessage);
        }

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.File))
        {
            FileQueue.Enqueue(unFormatedMessage + Environment.NewLine);
            FileQueueSignal.Set();
        }

        if (!OperatingSystem.IsWindows())
            return;

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.Pipes) && _settings.PipeSettings is { UsePipeLogging: true } && _settings.HasPipeConfig())
        {
            PipeQueue.Enqueue(consoleMessage);
            PipeQueueSignal.Set();
        }

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.EventLog) && _settings.HasEventLogConfig() && EventLog.SourceExists(_settings.EventLogSource))
        {
            EventLog.WriteEntry(_settings.EventLogSource, unFormatedMessage, GetEntryType(logLevel));
        }
    }

    [SupportedOSPlatform("windows")]
    private static EventLogEntryType GetEntryType(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => EventLogEntryType.Information,
        LogLevel.Debug => EventLogEntryType.Information,
        LogLevel.Information => EventLogEntryType.Information,
        LogLevel.Warning => EventLogEntryType.Warning,
        LogLevel.Error => EventLogEntryType.Error,
        LogLevel.Critical => EventLogEntryType.Error,
        LogLevel.None => EventLogEntryType.Information,
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
    };

    private static async Task ProcessLogQueue(string logFilePath)
    {
        while (true)
        {
            FileQueueSignal.WaitOne();

            while (FileQueue.TryDequeue(out string? log))
            {
                await File.AppendAllTextAsync(logFilePath, log).ConfigureAwait(false);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private static async Task ProcessPipeQueue(PipeSettings? pipeSettings)
    {
        ArgumentNullException.ThrowIfNull(pipeSettings, nameof(pipeSettings));

        await using NamedPipeClientStream pipeClient = new(pipeSettings.Value.ServerName, pipeSettings.Value.PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync().ConfigureAwait(false);
        await using StreamWriter writer = new(pipeClient);

        while (true)
        {
            PipeQueueSignal.WaitOne();

            while (PipeQueue.TryDequeue(out string? log))
            {
                if (pipeClient is { IsConnected: false, CanWrite: false })
                    continue;

                await writer.WriteLineAsync(log).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileTask?.Dispose();
        _pipeTask?.Dispose();
        GC.SuppressFinalize(this);
    }
}