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
    public bool IsInitiated { get; set; } = false;
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
      var p =
        _optionProps
          .FirstOrDefault(m => string.Equals(m.Name, op.Name, StringComparison.CurrentCultureIgnoreCase))
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
        null;
      return new PluginOptionsDTO
      {
        Name = op.Name,
        Default = p,
        Description = op.Description,
        OptType = maybeType,
        Multi = op.Argument.Arity.Equals(ArgumentArity.OneOrMore) || op.Argument.Arity.Equals(ArgumentArity.ZeroOrMore),
        Deprecated = false
      };
    }

    [JsonRpcMethod("hello")]
    public async Task<string> Hello(string name)
    {
      using var releaser = await _semaphore.EnterAsync();
      _logger.LogInformation("greeting: {Name} ... ", name);
      return $"hello!! {name}! This is {_opts.GreeterName} !!";
    }


    [JsonRpcMethod("init")]
    public async Task Init(LnInitConfigurationDTO configuration, Dictionary<string, object> options)
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
    public async Task<ManifestDto> GetManifest(bool allow_deprecated_apis = false, object otherParams = null)
    {
      using var releaser = await _semaphore.EnterAsync();
      var userDefinedMethodInfo =
        this
          .GetType()
          .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
          .Where(m => !m.IsSpecialName && !String.Equals(m.Name, "init", StringComparison.OrdinalIgnoreCase) && !String.Equals(m.Name, "getmanifest", StringComparison.OrdinalIgnoreCase));
      var methods =
        userDefinedMethodInfo
          .Select(m =>
          {
            var name = m.Name.ToLowerInvariant();
            var argSpec = m.GetParameters();
            var numDefaults =
              argSpec.Count(s => s.HasDefaultValue);
            var keywordArgsStartIndex = argSpec.Length - numDefaults;
            var args =
              argSpec.Where(s =>
                !string.Equals(s.Name, "plugin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(s.Name, "request", StringComparison.OrdinalIgnoreCase))
                .Select((s, i) => i < keywordArgsStartIndex ? s.Name : $"[{s.Name}]");

            return new RPCMethodDTO
            {
              Name = name,
              Usage = string.Join(' ', args),
              Description = _rpcDescriptions[name].Item1,
              LongDescription = _rpcDescriptions[name].Item1
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
