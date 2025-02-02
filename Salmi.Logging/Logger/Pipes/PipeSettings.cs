namespace Salmi.Logging.Logger.Pipes;

public struct PipeSettings(bool usePipeLogging, string pipeName, string serverName = ".")
{
    public bool UsePipeLogging { get; set; } = usePipeLogging;
    public string PipeName { get; set; } = pipeName;
    public string ServerName { get; set; } = serverName;
}