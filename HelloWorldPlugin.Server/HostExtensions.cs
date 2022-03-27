using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamJsonRpc;

namespace HelloWorldPlugin.Server
{
  public static class HostExtensions
  {
    public static Task StartJsonRpcServerForInitAsync(this IHost host)
    {
      var sp = host.Services;
      var rpcServer = sp.GetRequiredService<GreeterJsonRpcServer>();
      var formatter = new JsonMessageFormatter();
      var handler = new NewLineDelimitedMessageHandler(Console.OpenStandardOutput(), Console.OpenStandardInput(), formatter);
      var rpc = new JsonRpc(handler);
      rpc.AddLocalRpcTarget(rpcServer, new JsonRpcTargetOptions());
      rpc.ExceptionStrategy = ExceptionProcessing.CommonErrorData;
      rpc.StartListening();

      var opts = sp.GetRequiredService<JsonRpcServerOptions>();
      while (!opts.IsInitiated) {}

      return Task.CompletedTask;
    }
  }
}
