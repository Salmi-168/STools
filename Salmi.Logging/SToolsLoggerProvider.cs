using Microsoft.Extensions.Logging;
using Salmi.Logging.Logger;

namespace Salmi.Logging;

public class SToolsLoggerProvider(SToolLoggerSettings settings) : ILoggerProvider
{
    private readonly List<SToolLogger> _loggers = [];

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        SToolLogger logger = new(categoryName, settings);
        _loggers.Add(logger);

        return logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (SToolLogger logger in _loggers)
        {
            logger.LogDebug("Disposing logger \"{name}\"", logger.Name);
            logger.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}