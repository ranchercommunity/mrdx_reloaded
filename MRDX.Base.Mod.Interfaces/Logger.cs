using System.Collections.Generic;
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

    public static ILogger? LoggerInternal { get; set; }

    public static void SetLogLevel(LogLevel level)
    {
        LogLevels[Assembly.GetCallingAssembly().FullName ?? ""] = level;
    }

    public static void Error(string message, Color? color = null)
    {
        Write(LogLevel.Error, Assembly.GetCallingAssembly().FullName ?? "", message, color ?? Color.Red);
    }

    public static void Warn(string message, Color? color = null)
    {
        Write(LogLevel.Warning, Assembly.GetCallingAssembly().FullName ?? "", message, Color.Yellow);
    }

    public static void Info(string message, Color? color = null)
    {
        Write(LogLevel.Info, Assembly.GetCallingAssembly().FullName ?? "", message, color ?? Color.White);
    }

    public static void Debug(string message, Color? color = null)
    {
        Write(LogLevel.Debug, Assembly.GetCallingAssembly().FullName ?? "", message, color ?? Color.LightGreen);
    }

    public static void Trace(string message, Color? color = null)
    {
        Write(LogLevel.Trace, Assembly.GetCallingAssembly().FullName ?? "", message, color ?? Color.LightBlue);
    }

    public static void Write(LogLevel level, string tag, string message, Color c)
    {
        var baseLevel = LogLevels.TryGetValue(tag, out var l) ? LogLevel.Warning : l;
        if ((int)level > (int)baseLevel) return;
        LoggerInternal?.WriteLineAsync($"[{tag}] ${level.ToString()}: {message}", c);
    }
}