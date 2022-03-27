# Example c-lightning plugin for .NET

### How to

With dotnet sdk 6.x installed on your computer, run
`./build_plugin.sh`

And you will have single binary in `./publish/greetd`.
This is a plugin itself, you can pass to `lightningd` with `--plugin` or `--plugin-dir` option.

To see startup options, run `./publish/greetd --help`

