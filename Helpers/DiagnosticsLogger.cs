using System;
using System.Diagnostics;
using System.IO;

namespace Pet.TaskDevourer.Helpers
{
    internal static class DiagnosticsLogger
    {
        private static readonly object _lock = new();
        private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");

        public static void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                lock (_lock)
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                Debug.WriteLine(line);
            }
            catch {}
        }
    }
}
