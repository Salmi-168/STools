using Microsoft.Extensions.Logging;

namespace Salmi.Logging.Extensions;

public static class SToolLoggingExtension
{
    public static ILoggingBuilder AddSToolLogger(this ILoggingBuilder loggingBuilder, Action<SToolLoggerSettings> configureLogger)
    {
        SToolLoggerSettings settings = new();
        configureLogger(settings);

        if (settings.SToolLoggerType == SToolLoggerType.None)
            return loggingBuilder;

        loggingBuilder.AddProvider(new SToolsLoggerProvider(settings));

        return loggingBuilder;
    }
}