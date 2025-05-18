using System;
using System.IO;
using System.Threading;

namespace RavenHub
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private const int MaxLogFileSizeMB = 5;
        private const int KeepLogDays = 7;

        private static string LogDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static string CurrentLogFilePath => GetCurrentLogFilePath();

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                CleanOldLogs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOGGER INIT FAILED: {ex.Message}");
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (_lock)
            {
                try
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] - {message}\n";
                    File.AppendAllText(CurrentLogFilePath, logEntry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LOG WRITE FAILED: {ex.Message}");
                }
            }
        }

        public static void Log(Exception ex, string context = null)
        {
            string message = $"{(context != null ? $"Context: {context}\n" : "")}" +
                           $"EXCEPTION: {ex.GetType().Name}\n" +
                           $"Message: {ex.Message}\n" +
                           $"Stack: {ex.StackTrace}";

            Log(message, LogLevel.Error);
        }

        private static string GetCurrentLogFilePath()
        {
            string baseName = $"log_{DateTime.Now:yyyy-MM-dd}";
            string path = Path.Combine(LogDirectory, $"{baseName}.txt");

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length > MaxLogFileSizeMB * 1024 * 1024)
                {
                    int counter = 1;
                    while (File.Exists(Path.Combine(LogDirectory, $"{baseName}_{counter}.txt")))
                    {
                        counter++;
                    }
                    path = Path.Combine(LogDirectory, $"{baseName}_{counter}.txt");
                }
            }

            return path;
        }

        private static void CleanOldLogs()
        {
            try
            {
                foreach (var file in Directory.GetFiles(LogDirectory, "log_*.txt"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-KeepLogDays))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOG CLEAN FAILED: {ex.Message}");
            }
        }
    }

    public enum LogLevel
    {
        Debug,    // Отладочная информация
        Info,     // Обычные сообщения
        Warning,  // Предупреждения
        Error     // Ошибки
    }
}