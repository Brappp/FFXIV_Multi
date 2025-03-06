using System;
using System.IO;
using System.Text;
using System.Threading;

namespace FFXIVClientManager.Utils
{
    /// <summary>
    /// Helper for application logging
    /// </summary>
    public class LogHelper
    {
        private readonly string _logFilePath;
        private readonly object _lockObj = new object();
        private readonly bool _consoleOutput;

        public event EventHandler<LogEventArgs> LogEntryAdded;

        /// <summary>
        /// Initialize a new LogHelper instance
        /// </summary>
        /// <param name="logDirectory">Directory to store log files</param>
        /// <param name="consoleOutput">Whether to output logs to console (for debugging)</param>
        public LogHelper(string logDirectory, bool consoleOutput = false)
        {
            _consoleOutput = consoleOutput;

            if (string.IsNullOrEmpty(logDirectory))
            {
                logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FFXIVClientManager", "Logs");
            }

            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            _logFilePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

            // Write startup message to log
            WriteToLog("INFO", "===== Application started =====");
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public void LogInfo(string message)
        {
            WriteToLog("INFO", message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void LogWarning(string message)
        {
            WriteToLog("WARNING", message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void LogError(string message, Exception ex = null)
        {
            WriteToLog("ERROR", message);

            if (ex != null)
            {
                WriteToLog("ERROR", $"Exception: {ex.Message}");
                WriteToLog("ERROR", $"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Log a debug message (only in debug builds)
        /// </summary>
        public void LogDebug(string message)
        {
#if DEBUG
            WriteToLog("DEBUG", message);
#endif
        }

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        private void WriteToLog(string level, string message)
        {
            try
            {
                var timestamp = DateTime.Now;
                var formattedMessage = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] [{Thread.CurrentThread.ManagedThreadId}] {message}";

                lock (_lockObj)
                {
                    using (var writer = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(formattedMessage);
                    }
                }

                // Output to console if enabled
                if (_consoleOutput)
                {
                    var originalColor = Console.ForegroundColor;

                    switch (level)
                    {
                        case "ERROR":
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case "WARNING":
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case "DEBUG":
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }

                    Console.WriteLine(formattedMessage);
                    Console.ForegroundColor = originalColor;
                }

                // Notify listeners
                OnLogEntryAdded(level, message, timestamp);
            }
            catch
            {
                // Ignore errors in logging
            }
        }

        /// <summary>
        /// Gets the contents of the current log file
        /// </summary>
        public string GetLogContents()
        {
            try
            {
                lock (_lockObj)
                {
                    if (File.Exists(_logFilePath))
                    {
                        return File.ReadAllText(_logFilePath);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the path to the current log file
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Cleans up old log files
        /// </summary>
        public void CleanupOldLogs(int daysToKeep = 7)
        {
            try
            {
                string logDirectory = Path.GetDirectoryName(_logFilePath);

                if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in Directory.GetFiles(logDirectory, "log_*.txt"))
                {
                    var fileInfo = new FileInfo(file);

                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore errors deleting individual files
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors cleaning up logs
            }
        }

        #region Event Handlers

        protected virtual void OnLogEntryAdded(string level, string message, DateTime timestamp)
        {
            LogEntryAdded?.Invoke(this, new LogEventArgs(level, message, timestamp));
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for log entries
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        public string Level { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string level, string message, DateTime timestamp)
        {
            Level = level;
            Message = message;
            Timestamp = timestamp;
        }
    }
}