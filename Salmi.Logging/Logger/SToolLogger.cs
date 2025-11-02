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

        if (_settings.LogToFileEnabled)
            _fileTask = Task.Run(ProcessLogQueue);

        if (OperatingSystem.IsWindows() && _settings.LogToPipesEnabled && _settings.HasPipeConfig())
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
        string exceptionText = (exception is not null ? Environment.NewLine + exception : string.Empty) + (exception?.InnerException is not null ? Environment.NewLine + exception.InnerException : string.Empty);
        string unFormatedMessage = dateTime + " " + $"[{logLevel, 11}]" + $" {name} - {state + exceptionText}";
        string consoleMessage = dateTime + " " + $"[{logLevel.ToColorCodedConsoleString(true)}]" + $" {name} - {(state + exceptionText).ToColorCodedConsoleString(logLevel)}";

        if (_settings.LogToConsoleEnabled)
        {
            Console.WriteLine(_settings.ColoredConsoleOutput ? consoleMessage : unFormatedMessage);
        }

        if (_settings.LogToFileEnabled)
        {
            FileQueue.Enqueue(unFormatedMessage + Environment.NewLine);
            FileQueueSignal.Set();
        }

        if (!OperatingSystem.IsWindows())
            return;

        if (_settings is { LogToPipesEnabled: true, PipeSettings.UsePipeLogging: true } && _settings.HasPipeConfig())
        {
            PipeQueue.Enqueue(_settings.ColoredConsoleOutput ? consoleMessage : unFormatedMessage);
            PipeQueueSignal.Set();
        }

        if (_settings.LogToEventLogEnabled && _settings.HasEventLogConfig() && EventLog.SourceExists(_settings.EventLogSource))
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