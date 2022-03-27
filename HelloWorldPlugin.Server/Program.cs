

using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using CLightningPlugin;
using HelloWorldPlugin.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var rc = CommandLines.GetRootCommand();
Action<IHostBuilder> configureHostBuilder = hostBuilder =>
{
  var isPluginMode = Environment.GetEnvironmentVariable("LIGHTNINGD_PLUGIN") == "1";

  if (isPluginMode)
  {
    hostBuilder
      .ConfigureLogging(builder => { builder.AddJsonRpcNotificationLogger();})
      .ConfigureServices(serviceCollection =>
        serviceCollection
          .AddSingleton<JsonRpcServerOptions>()
          .AddSingleton<GreeterJsonRpcServer>()
      );
  }
  else
  {
    var url = "https://www.tpeczek.com/2020/06/json-rpc-in-aspnet-core-with.html";
    throw new NotSupportedException($"Running this plugin as a regular web server is possible, but currently not supported. See ref: {url} for how to");
  }
};

var useWebHostMiddleware = new InvocationMiddleware(async (ctx, next) =>
{
  var hostBuilder = new HostBuilder();
  hostBuilder.Properties[typeof(InvocationContext)] = ctx;
  hostBuilder.ConfigureServices(services =>
      services
        .AddSingleton(ctx)
        .AddSingleton(ctx.BindingContext)
        .AddSingleton(ctx.Console)
        .AddTransient(_ => ctx.InvocationResult!)
        .AddTransient(_ => ctx.ParseResult)
    )
    .UseInvocationLifetime(ctx);
  configureHostBuilder(hostBuilder);
  var host = hostBuilder.Build();
  ctx.BindingContext.AddService(typeof(IHost), _ => host);
  await next.Invoke(ctx);
  var isPluginMode = Environment.GetEnvironmentVariable("LIGHTNINGD_PLUGIN") == "1";
  if (isPluginMode)
    await host.StartJsonRpcServerForInitAsync();
  await host.RunAsync();
});

var cb = new CommandLineBuilder(rc);
cb
  .UseDefaults()
  .UseMiddleware(useWebHostMiddleware)
  .Build()
  .Invoke(args);

