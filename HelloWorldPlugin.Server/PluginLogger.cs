using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace HelloWorldPlugin.Server
{

  public class JsonRpcNotificationLoggerOptions
  {
    public JsonSerializerSettings? JsonSettings { get; set; }
  }

  public class JsonRpcNotificationLoggerProvider : ILoggerProvider
  {
    private readonly JsonRpcNotificationLoggerOptions _opts;

    public JsonRpcNotificationLoggerProvider(Action<JsonRpcNotificationLoggerOptions> configure)
    {
      if (configure == null) throw new ArgumentNullException(nameof(configure));
      var set = new JsonRpcNotificationLoggerOptions();
      configure(set);
      _opts = set;
    }

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName) =>
      new JsonRpcNotificationLogger(categoryName, (_, _) => true, null, _opts);
  }

  /// Similar to ConsoleLogger, JsonRpcNotificationLoggerProvider outputs log messages to stdout.
  /// But it has 2 differences.
  /// 1. it does not enqueue the messages to background thread worker, and instead
  ///    it processes one-by-one. this will cause slight performance degradation.
  /// 2. It outputs as a JSON-RPC notification
  /// As a result, it is ideal for sending log message back to host clightning. Which will
  /// in turn logs message to stdout.
  public class JsonRpcNotificationLogger: ILogger
  {
    private readonly JsonRpcNotificationLoggerOptions _opts;

    public JsonRpcNotificationLogger(string name, Func<string, LogLevel, bool>? filter, bool includeScopes, JsonRpcNotificationLoggerOptions opts)
      : this(name, filter, includeScopes ? new LoggerExternalScopeProvider() : null, opts)
    {
    }

    internal JsonRpcNotificationLogger(string name, Func<string, LogLevel, bool>? filter, IExternalScopeProvider? scopeProvider, JsonRpcNotificationLoggerOptions opts)
    {
      _opts = opts;
      Name = name ?? throw new ArgumentNullException(nameof(name));
      Filter = filter ?? ((_, _) => true);
      ScopeProvider = scopeProvider;
    }

    public IExternalScopeProvider? ScopeProvider { get; set; }

    public Func<string, LogLevel, bool> Filter { get; }

    public string Name { get; set; }

    private void WriteMsg(LogLevel logLevel, string logName, string msg, Exception? exception)
    {
      var json = Newtonsoft.Json.JsonSerializer.Create( _opts.JsonSettings );
      foreach (var line in msg.Split("\n"))
      {
        var message = $"{logName}: {line}";
        var req = new JsonRpcRequest
        {
          Method = "log",
          NamedArguments = new Dictionary<string, object?> {{"level", GetLogLevelString(logLevel)}, { "message", message }}
        };
        json.Serialize(Console.Out, req);
        Console.Out.WriteLine();
      }

      if(exception != null)
      {
        var code =
          exception is LocalRpcException localRpcException ? localRpcException.ErrorCode :
            (int)JsonRpcErrorCode.InternalError;
        // exception message
        var message = $"{logName}: {exception.Message}";
        var req = new JsonRpcRequest
        {
          NamedArguments = new Dictionary<string, object?>
          {
            {
              "error", new Dictionary<string, object> {
                { "code", code },
                { "message", message },
                { "traceback", exception.StackTrace! }
              }
            }
          }
        };
        json.Serialize(Console.Out, req);
        Console.Out.WriteLine();
      }
      Console.Out.Flush();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
      if (!IsEnabled(logLevel))
        return;

      if (formatter == null)
        throw new ArgumentNullException(nameof(formatter));

      var msg = formatter(state, exception);
      if (!String.IsNullOrEmpty(msg) || exception != null)
        WriteMsg(logLevel, Name, msg, exception);
    }

    private static string GetLogLevelString(LogLevel logLevel) =>
      logLevel switch
      {
        LogLevel.Trace => "debug",
        LogLevel.Debug => "debug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "error",
        LogLevel.Critical => "error",
        _ =>
          throw new ArgumentOutOfRangeException(nameof(logLevel))
      };

    public bool IsEnabled(LogLevel logLevel) =>
      logLevel != LogLevel.None && Filter(Name, logLevel);

    public IDisposable BeginScope<TState>(TState state) =>
      ScopeProvider?.Push(state) ?? NullScope.Instance;

    /// <summary>
    /// An empty scope without any logic
    /// </summary>
    public class NullScope : IDisposable
    {
      public static NullScope Instance { get; } = new NullScope();

      private NullScope()
      {
      }

      /// <inheritdoc />
      public void Dispose()
      {
      }
    }
  }

}
