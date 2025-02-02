using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Salmi.Logging.Logger;

namespace Salmi.Logging.Extensions;

public static class SToolLoggingExtension
{
    public static ILoggingBuilder AddSToolLogger(this ILoggingBuilder builder, Action<SToolLoggerSettings> configureLogger)
    {
        SToolLoggerSettings settings = new();
        configureLogger(settings);

        if (settings.SToolLoggerType == SToolLoggerType.None)
            return builder;

        builder.AddProvider(new SToolsLoggerProvider(settings));

        return builder;
    }
}