$ErrorActionPreference = "Stop"

$RUNTIMES = @(
    "linux-x64",
    "win-x64",
    "osx-x64",
    "osx-arm64"
)

foreach ($runtime in $RUNTIMES) {
    dotnet publish `
        -c Release `
        -r $runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -o "dist/$runtime"
}
