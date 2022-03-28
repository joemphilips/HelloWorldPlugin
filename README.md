# Example c-lightning plugin for .NET

Please check [my blogpost](https://dev.to/joemphilips/building-c-lightning-plugin-with-net-3162) for what this repo is about.

### How to

With dotnet sdk 6.x installed on your computer, run
`./build_plugin.sh`

And you will have single binary in `./publish/greetd`.
This is a plugin itself, you can pass to `lightningd` with `--plugin` or `--plugin-dir` option.

To see the startup options, run `./publish/greetd --help`

### testing with docker

```bash
docker-compose up --build lightningd

# check rpc for the plugin is added to c-lightning.
./docker-lightning-cli.sh | grep hello
```

