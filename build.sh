#!/usr/bin/env bash

set -e

RUNTIMES=(
    "linux-x64"
    "win-x64"
    "osx-x64"
    "osx-arm64"
)

for runtime in "${RUNTIMES[@]}"; do
    dotnet publish \
        -c Release \
        -r "$runtime" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -o "dist/$runtime"
done
