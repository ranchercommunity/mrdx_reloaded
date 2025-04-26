using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.Mod.Interfaces;

public static class Logger
{
    public enum LogLevel
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Debug = 3,
        Trace = 4
    }

    private static readonly Dictionary<string, LogLevel> LogLevels = new();
    private static ILogger? _logger;

    public static LogLevel GlobalLogLevel { get; set; } = LogLevel.Error;

    public static ILogger? LoggerInternal
    {
        get => _logger;
        set
        {
            GlobalLogLevel = Debugger.IsAttached ? LogLevel.Trace : LogLevel.Info;
            _logger = value;
        }
    }

    public static void SetLogLevel(LogLevel level)
    {
        LogLevels[Assembly.GetCallingAssembly().FullName ?? ""] = level;
    }

    public static void Error(string message, Color? color = null)
    {
        Write(LogLevel.Error, Assembly.GetCallingAssembly().GetName().Name ?? "", message, color ?? Color.Red);
    }

    public static void Warn(string message, Color? color = null)
    {
        Write(LogLevel.Warning, Assembly.GetCallingAssembly().GetName().Name ?? "", message, Color.Yellow);
    }

    public static void Info(string message, Color? color = null)
    {
        Write(LogLevel.Info, Assembly.GetCallingAssembly().GetName().Name ?? "", message, color ?? Color.White);
    }

    public static void Debug(string message, Color? color = null)
    {
        Write(LogLevel.Debug, Assembly.GetCallingAssembly().GetName().Name ?? "", message, color ?? Color.LightGreen);
    }

    public static void Trace(string message, Color? color = null)
    {
        Write(LogLevel.Trace, Assembly.GetCallingAssembly().GetName().Name ?? "", message, color ?? Color.LightBlue);
    }

    public static void Write(LogLevel level, string tag, string message, Color c)
    {
        var baseLevel = LogLevels.GetValueOrDefault(tag, GlobalLogLevel);
        if ((int)level > (int)baseLevel) return;
        LoggerInternal?.WriteLineAsync($"[{tag}] {level.ToString()}: {message}", c);
    }
}