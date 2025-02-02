using System.Drawing;
using System.Text;
using Microsoft.Extensions.Logging;
using Pastel;

namespace Salmi.Logging.Helper;

internal static class ConsoleExtensions
{
    public static string SetLength(this string s, int stringLenght)
    {
        if (s.Length >= stringLenght)
            return s.Length > stringLenght ? s[..stringLenght] : s;

        StringBuilder builder = new(s);
        builder.Append(' ', stringLenght - s.Length);

        return builder.ToString();
    }

    public static string ToColorCodedConsoleString(this string logLevelString, LogLevel logLevel) => logLevelString.Pastel(logLevel.ToColor());
    public static string ToColorCodedConsoleString(this LogLevel logLevel) => logLevel.ToString().Pastel(logLevel.ToColor());

    public static Color ToColor(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => Color.FromArgb(0, 255, 0),
            LogLevel.None or LogLevel.Information => Color.FromArgb(204, 204, 204),
            LogLevel.Warning => Color.FromArgb(255, 200, 0),
            LogLevel.Error or LogLevel.Critical => Color.FromArgb(255, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }
}