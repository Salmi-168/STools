using System.Runtime.Versioning;

namespace Salmi.Logging.Logger;

[Flags]
public enum SToolLoggerType
{
    /// <summary>
    /// No Logging
    /// </summary>
    None = 0,

    /// <summary>
    /// Use standard console output
    /// </summary>
    Console = 1,

    /// <summary>
    /// Log to a file
    /// </summary>
    File = 2,

    /// <summary>
    /// Log to the Windows Event Log (Windows only)
    /// </summary>
    EventLog = 4,

    /// <summary>
    /// Use Windows Pipes for logging (Windows only)
    /// </summary>
    Pipes = 8,

    /// <summary>
    /// Log to all available logging types except for those that are Windows only
    /// </summary>
    AllNoneWindows = Console | File,

    /// <summary>
    /// Log to all available logging types
    /// </summary>
    All = Console | File | EventLog | Pipes
}