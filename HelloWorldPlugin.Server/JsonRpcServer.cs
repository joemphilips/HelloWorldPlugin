using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace HelloWorldPlugin.Server
{

  public class JsonRpcServerOptions
  {
    public string GreeterName { get; set; } = "World";
    public bool IsInitiated { get; set; }
    public static readonly JsonRpcServerOptions Instance =
      new JsonRpcServerOptions();
  }


  public class GreeterJsonRpcServer
  {
    private readonly ILogger<GreeterJsonRpcServer> _logger;
    private readonly JsonRpcServerOptions _opts;

    private readonly AsyncSemaphore _semaphore = new(1);

    private static Dictionary<string, (string, string)> _rpcDescriptions = new()
    {
      { "hello", ("reply to an user by saying hello", $"Reply to an user by saying hello. The reply message contains the server name, which you can specify by {nameof(JsonRpcServerOptions.Instance.GreeterName).ToLowerInvariant()} on startup") }
    };
    private readonly JsonRpcServerOptions _options;
    private readonly PropertyInfo[] _optionProps;

    public GreeterJsonRpcServer(ILogger<GreeterJsonRpcServer> logger, JsonRpcServerOptions opts)
    {
      this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _opts = opts;
      _options = opts;
      _optionProps =
        JsonRpcServerOptions
          .Instance
          .GetType()
          .GetProperties();
    }

    private PluginOptionsDTO CliOptionsToDto(Option op)
    {
      var ty =
        op.Argument.ArgumentType;
      var maybeDefaultValue =
        _optionProps
          .FirstOrDefault(m => string.Equals(m.Name, op.Name, StringComparison.OrdinalIgnoreCase))
          ?.GetValue(_options);
      var maybeType =
        ty == typeof(int) ? "int" :
        ty == typeof(int[]) ? "int" :
        ty == typeof(string) ? "string" :
        ty == typeof(string[]) ? "string" :
        ty == typeof(FileInfo) ? "string" :
        ty == typeof(FileInfo[]) ? "string" :
        ty == typeof(DirectoryInfo) ? "string" :
        ty == typeof(DirectoryInfo[]) ? "string" :
        ty == typeof(bool) ? "string" :
        ty == typeof(bool[]) ? "string" :
        op.Argument.Arity.Equals(ArgumentArity.Zero) ? "flag" :
        throw new InvalidDataException($"argument with type {ty} is not supported");
      _logger.LogInformation($"opts: Name: {op.Name}. Desc: {op.Description}. type: {maybeType}");
      return new PluginOptionsDTO
      {
        Name = op.Name,
        Default = maybeDefaultValue,
        Description = op.Description ?? throw new Exception($"You must pass description for option {op.Name}"),
        OptType = maybeType,
        Multi = op.Argument.Arity.Equals(ArgumentArity.OneOrMore) || op.Argument.Arity.Equals(ArgumentArity.ZeroOrMore),
        Deprecated = false
      };
    }

    [JsonRpcMethod("hello")]
    public async Task<string> HelloAsync(string name)
    {
      using var releaser = await _semaphore.EnterAsync();
      _logger.LogInformation("greeting: {Name} ... ", name);
      return $"hello!! {name}! This is {_opts.GreeterName} !!";
    }


    [JsonRpcMethod("init")]
    public async Task InitAsync(LnInitConfigurationDTO configuration, Dictionary<string, object> options)
    {
      using var releaser = await _semaphore.EnterAsync();
      foreach (var op in options)
      {
        var maybeProp =
          _opts.GetType().GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, op.Key, StringComparison.OrdinalIgnoreCase));
        maybeProp?.SetValue(_opts, op.Value);
      }
      _opts.IsInitiated = true;
    }

    [JsonRpcMethod("getmanifest")]
    public async Task<ManifestDto> GetManifestAsync(bool allowDeprecatedApis = false, object? otherParams = null)
    {
      using var releaser = await _semaphore.EnterAsync();
      var userDefinedMethodInfo =
        this
          .GetType()
          .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
          .Where(m => !m.IsSpecialName && !String.Equals(m.Name, "initasync", StringComparison.OrdinalIgnoreCase) && !String.Equals(m.Name, "getmanifestasync", StringComparison.OrdinalIgnoreCase));
      var methods =
        userDefinedMethodInfo
          .Select(m =>
          {
            var argSpec = m.GetParameters();
            var numDefaults =
              argSpec.Count(s => s.HasDefaultValue);
            var keywordArgsStartIndex = argSpec.Length - numDefaults;
            var args =
              argSpec.Where(s =>
                !string.Equals(s.Name, "plugin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(s.Name, "request", StringComparison.OrdinalIgnoreCase))
                .Select((s, i) => i < keywordArgsStartIndex ? s.Name : $"[{s.Name}]");

            var name = m.Name.ToLowerInvariant();
            if (name.EndsWith("async"))
              name = name[..^5];
            return new RPCMethodDTO
            {
              Name = name,
              Usage = string.Join(' ', args),
              Description = _rpcDescriptions[name].Item1,
              LongDescription = _rpcDescriptions[name].Item2
            };
          });

      return new ManifestDto
      {
        Options =
          CommandLines.GetOptions().Select(CliOptionsToDto).ToArray(),
        RpcMethods = methods.ToArray(),
        Notifications = new NotificationsDTO[]{},
        Subscriptions = new string[]{},
        Hooks = new object[] {},
        Dynamic = true,
        FeatureBits = null
      };
    }
  }
}
