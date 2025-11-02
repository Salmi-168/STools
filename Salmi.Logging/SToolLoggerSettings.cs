using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Salmi.Logging.Logger.Pipes;

namespace Salmi.Logging;

public class SToolLoggerSettings
{
	public SToolLoggerType SToolLoggerType;

	internal bool LogToConsoleEnabled => SToolLoggerType.HasFlag(SToolLoggerType.Console);
	internal bool LogToFileEnabled => SToolLoggerType.HasFlag(SToolLoggerType.File);
	internal bool LogToEventLogEnabled => SToolLoggerType.HasFlag(SToolLoggerType.EventLog);
	internal bool LogToPipesEnabled => SToolLoggerType.HasFlag(SToolLoggerType.Pipes);

	public string LogFilePath { get; set; } = (Process.GetCurrentProcess().MainModule?.FileName[..^4] ?? $"Unnamed-Log-{DateTime.Now:dd:MM:yyyy_HH:mm:ss:}") + ".log";

	/// <summary>
	/// When false the message is not colored. (Also in pipes.)<br/>
	/// Default is true.
	/// </summary>
	public bool ColoredConsoleOutput { get; set; } = true;

	/// <summary>
	/// Configure the pipe settings for the logger. (Pipes are Windows only)<br/>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	[SupportedOSPlatform("Windows")]
	public PipeSettings? PipeSettings { get; set; }

	/// <summary>
	/// Check if the logger has pipe settings.<br/>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	[SupportedOSPlatform("Windows")]
	[MemberNotNullWhen(true, nameof(PipeSettings))]
	public bool HasPipeConfig() => PipeSettings is not null;

	/// <summary>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	[SupportedOSPlatform("Windows")]
	public string? EventLogSource { get; set; }

	/// <summary>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	[SupportedOSPlatform("Windows")]
	[MemberNotNullWhen(true, nameof(EventLogSource))]
	public bool HasEventLogConfig() => EventLogSource is not null;
}