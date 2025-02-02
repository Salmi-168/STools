using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Salmi.Logging.Logger.Pipes;

namespace Salmi.Logging.Logger;

public class SToolLoggerSettings
{
	public SToolLoggerType SToolLoggerType;

	public string LogFilePath { get; set; } = (Process.GetCurrentProcess().MainModule?.FileName[..^4] ?? $"Unnamed-Log-{DateTime.Now:dd:MM:yyyy_HH:mm:ss:}") + ".log";

	/// <summary>
	/// Configure the pipe settings for the logger.<br/>
	/// (Pipes are Windows only)<br/>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	public PipeSettings? PipeSettings { get; set; }

	/// <summary>
	/// Check if the logger has pipe settings.<br/>
	/// Use <see cref="OperatingSystem.IsWindows()"/> to check if the current OS is Windows and remove the warnings.
	/// </summary>
	[SupportedOSPlatform("Windows")]
	[MemberNotNullWhen(true, nameof(PipeSettings))]
	public bool HasPipeConfig() => PipeSettings != null;

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
	public bool HasEventLogConfig() => EventLogSource != null;
}