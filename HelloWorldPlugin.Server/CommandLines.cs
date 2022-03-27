using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using StreamJsonRpc;

namespace HelloWorldPlugin.Server
{
  public static class CommandLines
  {

    public static Option[] GetOptions()
    {
      var op =
        new Option<string>($"--{nameof(JsonRpcServerOptions.Instance.GreeterName).ToLowerInvariant()}")
        {
          Description = "The name of server of which users will see by calling `hello` method",
          Argument = new Argument<string>
          {
            Arity = ArgumentArity.ZeroOrOne
          }
        };
      op.Argument.SetDefaultValue(JsonRpcServerOptions.Instance.GreeterName);
      return new Option[]
      {
        op
      };
    }

    public static RootCommand GetRootCommand()
    {
      var rc =
        new RootCommand
        {
          Name = "greetd",
          Description = "The name of server of which users will see by calling `hello` method"
        };

      foreach (var op in GetOptions())
        rc.AddOption(op);
      return rc;
    }
  }
}
