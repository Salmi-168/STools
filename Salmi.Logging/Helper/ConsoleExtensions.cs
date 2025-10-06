using System.Drawing;
using Microsoft.Extensions.Logging;
using Pastel;

namespace Salmi.Logging.Helper;

internal static class ConsoleExtensions
{
    private static readonly (Color Color, int PastelLenght) Green = CreateColor(Color.FromArgb(0, 255, 0));
    private static readonly (Color Color, int PastelLenght) White = CreateColor(Color.FromArgb(204, 204, 204));
    private static readonly (Color Color, int PastelLenght) Yellow = CreateColor(Color.FromArgb(255, 200, 0));
    private static readonly (Color Color, int PastelLenght) Red = CreateColor(Color.FromArgb(255, 0, 0));
    private static (Color Color, int PastelLenght) CreateColor(Color color) => (color, string.Empty.Pastel(color).Length);

    public static string ToColorCodedConsoleString(this string message, LogLevel logLevel) => message.Pastel(logLevel.ToColor());
    public static string ToColorCodedConsoleString(this LogLevel logLevel, bool applyLeftPad = false) => logLevel.ToString().Pastel(logLevel.ToColor()).PadLeft(11 + (applyLeftPad ? logLevel.GetPastelLength() : 0));

    private static Color ToColor(this LogLevel logLevel) => logLevel.GetColorAndLength().Color;
    private static int GetPastelLength(this LogLevel logLevel) => logLevel.GetColorAndLength().PastelLenght;
    private static (Color Color, int PastelLenght) GetColorAndLength(this LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => Green,
            LogLevel.None or LogLevel.Information => White,
            LogLevel.Warning => Yellow,
            LogLevel.Error or LogLevel.Critical => Red,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
}