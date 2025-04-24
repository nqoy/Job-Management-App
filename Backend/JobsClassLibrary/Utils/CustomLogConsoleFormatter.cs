using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace JobsClassLibrary.Utils
{
    public class CustomConsoleFormatter : ConsoleFormatter
    {
        public const string FormatterName = "Custom";

        public CustomConsoleFormatter() : base(FormatterName)
        {
        }
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string time = DateTime.UtcNow.ToString("dd-MM-yy HH:mm:ss");
            string levelName = logEntry.LogLevel.ToString();

            if (levelName.Length > 4)
                levelName = levelName.Substring(0, 4);
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            string prefix = $"[{time}]-[{levelName}] :";

            textWriter.WriteLine($"{prefix} {message}");
        }
    }
}
