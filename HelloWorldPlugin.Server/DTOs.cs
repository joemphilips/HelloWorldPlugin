namespace HelloWorldPlugin.Server
{

  public class ProxyDTO
  {
    [Newtonsoft.Json.JsonProperty("type")]
    public string Ty { get; set; }
    [Newtonsoft.Json.JsonProperty("address")]
    public string Address { get; set; }
    [Newtonsoft.Json.JsonProperty("port")]
    public int Port { get; set; }
  }

  public class NotificationsDTO
  {
    [Newtonsoft.Json.JsonProperty("method")]
    public string Method { get; set; }
  }

  public class FeatureSetDTO
  {
    [Newtonsoft.Json.JsonProperty("init")]
    public string? Init { get; set; }
    [Newtonsoft.Json.JsonProperty("node")]
    public string? Node { get; set; }
    [Newtonsoft.Json.JsonProperty("channel")]
    public string? Channel { get; set; }
    [Newtonsoft.Json.JsonProperty("invoice")]
    public string? Invoice { get; set; }
  }

  public class PluginOptionsDTO
  {
    [Newtonsoft.Json.JsonProperty("name")]
    public string Name { get; set; }
    [Newtonsoft.Json.JsonProperty("default")]
    public object? Default { get; set; }
    [Newtonsoft.Json.JsonProperty("description")]
    public string Description { get; set; }

    [Newtonsoft.Json.JsonProperty("type")]
    public string OptType { get; set; }

    [Newtonsoft.Json.JsonProperty("multi")]
    public bool Multi { get; set; }

    [Newtonsoft.Json.JsonProperty("deprecated")]
    public bool Deprecated { get; set; }
  }


  public class LnInitConfigurationDTO
  {
    [Newtonsoft.Json.JsonProperty("lightning-dir")]
    public string? LightningDir { get; set; }

    [Newtonsoft.Json.JsonProperty("rpc-file")]
    public string? RpcFile { get; set; }

    [Newtonsoft.Json.JsonProperty("startup")]
    public bool Startup { get; set; }

    [Newtonsoft.Json.JsonProperty("network")]
    public string? Network { get; set; }

    [Newtonsoft.Json.JsonProperty("feature_set")]
    public FeatureSetDTO? FeatureSet { get; set; }

    [Newtonsoft.Json.JsonProperty("proxy")]
    public ProxyDTO? Proxy { get; set; }

    [Newtonsoft.Json.JsonProperty("torv3-enabled")]
    public bool TorV3Enabled { get; set; }

    [Newtonsoft.Json.JsonProperty("always_use_proxy")]
    public bool AlwaysUseProxy { get; set; }
  }

  public class RPCMethodDTO {
     [Newtonsoft.Json.JsonProperty("name")]
     public string Name { get; set; }

     [Newtonsoft.Json.JsonProperty("usage")]
     public string Usage { get; set; }

     [Newtonsoft.Json.JsonProperty("description")]
     public string Description { get; set; }

     [Newtonsoft.Json.JsonProperty("long_description")]
     public string LongDescription { get; set; }
  }

  public class ManifestDto
  {
    [Newtonsoft.Json.JsonProperty("options")]
    public PluginOptionsDTO[]? Options { get; set; }

    [Newtonsoft.Json.JsonProperty("rpcmethods")]
    public RPCMethodDTO[]? RpcMethods { get; set; }

    [Newtonsoft.Json.JsonProperty("subscriptions")]
    public string[]? Subscriptions { get; set; }

    [Newtonsoft.Json.JsonProperty("hooks")]
    public object[]? Hooks { get; set; }

    [Newtonsoft.Json.JsonProperty("dynamic")]
    public bool Dynamic { get; set; }

    [Newtonsoft.Json.JsonProperty("notifications")]
    public NotificationsDTO[]? Notifications {get; set; }

    [Newtonsoft.Json.JsonProperty("featurebits")]
    public FeatureSetDTO? FeatureBits { get; set; }
  }
}
