using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Salmi.Logging.Helper;

namespace Salmi.Logging.Logger;

internal class SToolLogger : ILogger, IDisposable
{
    internal string Name { get; }

    private readonly SToolLoggerSettings _settings;
    private readonly Task? _fileTask;
    private readonly Task? _pipeTask;

    internal SToolLogger(string name, SToolLoggerSettings settings)
    {
        Name = name;
        _settings = settings;

        if (_settings.SToolLoggerType.HasFlag(SToolLoggerType.File))
            _fileTask = Task.Run(ProcessLogQueue);

        if (OperatingSystem.IsWindows() && _settings.SToolLoggerType.HasFlag(SToolLoggerType.Pipes) && _settings.HasPipeConfig())
            _pipeTask = Task.Run(ProcessPipeQueue);
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

        string dateTime = $"[{DateTime.Now:HH:mm:ss HH:mm:ss}]";
        string name = $"[{Name, 30}]";
        string unFormatedMessage = dateTime + " " + $"[{logLevel, 11}]" + $" {name} - {formatter(state, exception)}";
        string consoleMessage = dateTime + " " + $"[{logLevel.ToColorCodedConsoleString(true)}]" + $" {name} - {formatter(state, exception).ToColorCodedConsoleString(logLevel)}";

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
        LogLevel.Trace or LogLevel.Debug or LogLevel.Information => EventLogEntryType.Information,
        LogLevel.Warning => EventLogEntryType.Warning,
        LogLevel.Error or LogLevel.Critical => EventLogEntryType.Error,
        LogLevel.None => EventLogEntryType.Information,
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
    };

    private async Task ProcessLogQueue()
    {
        while (true)
        {
            FileQueueSignal.WaitOne();

            while (FileQueue.TryDequeue(out string? log))
            {
                await File.AppendAllTextAsync(_settings.LogFilePath, log).ConfigureAwait(false);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private async Task ProcessPipeQueue()
    {
        if (!OperatingSystem.IsWindows() || _settings.PipeSettings is null)
            throw new InvalidOperationException("Pipe settings have not been set");

        await using NamedPipeClientStream pipeClient = new(_settings.PipeSettings.ServerName, _settings.PipeSettings.PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
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