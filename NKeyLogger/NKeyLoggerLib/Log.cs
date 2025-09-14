using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NKeyLoggerLib;

class FileLogger : ILogger
{

    private readonly string path;
    private readonly LogLevel logLevel;
    private object sync = new();
    public FileLogger(string path, LogLevel logLevel)
    {
        this.path = path;
        this.logLevel = logLevel;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => this.logLevel <= logLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (sync)
        {
            if (IsEnabled(logLevel))
                File.AppendAllText(this.path, formatter.Invoke(state, exception) + "\n");
        }
    }
}

class FileLoggerProvider : ILoggerProvider
{
    private readonly string path;
    public FileLoggerProvider(string path)
    {
        this.path = path;
    }
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(path, LogLevel.Information);
    }

    public void Dispose()
    {
        
    }
}

public class Log<T>
{
    private const string logPath = "Log.txt";
    public ILogger<T>? logger { get; private set; } = null;
    private static Log<T>? instance = null;
    public static Log<T> Instance
    {
        get
        {
            if(instance == null)
                instance = new Log<T>();
            return instance;
        }
    }
    private Log()
    {
        logger = LoggerFactory.Create( (ILoggingBuilder builder) =>
        {
#if DEBUG
            builder.AddConsole();
            builder.SetMinimumLevel( LogLevel.Debug );
#else
            builder.AddProvider(new FileLoggerProvider(logPath));
#endif
        }).CreateLogger<T>();
    }

}
