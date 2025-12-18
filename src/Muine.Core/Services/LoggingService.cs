using System;
using System.Diagnostics;
using System.IO;

namespace Muine.Core.Services;

/// <summary>
/// Simple logging service for Muine
/// </summary>
public static class LoggingService
{
    private static readonly object _lock = new object();
    private static string? _logFilePath;
    private static LogLevel _minimumLevel = LogLevel.Info;

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    static LoggingService()
    {
        // Initialize log file path
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Muine");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _logFilePath = Path.Combine(appDataPath, "muine.log");
    }

    public static void SetMinimumLevel(LogLevel level)
    {
        _minimumLevel = level;
    }

    public static void Debug(string message, string? category = null)
    {
        Log(LogLevel.Debug, message, category);
    }

    public static void Info(string message, string? category = null)
    {
        Log(LogLevel.Info, message, category);
    }

    public static void Warning(string message, string? category = null)
    {
        Log(LogLevel.Warning, message, category);
    }

    public static void Error(string message, Exception? exception = null, string? category = null)
    {
        var fullMessage = exception != null 
            ? $"{message} - {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
            : message;
        Log(LogLevel.Error, fullMessage, category);
    }

    private static void Log(LogLevel level, string message, string? category)
    {
        if (level < _minimumLevel)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
        var levelStr = level.ToString().ToUpper();
        var logMessage = $"{timestamp} [{levelStr}] {categoryStr}{message}";

        // Write to debug output
        System.Diagnostics.Debug.WriteLine(logMessage);

        // Write to file
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logFilePath!, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // If we can't write to log file, just ignore
        }
    }
}
