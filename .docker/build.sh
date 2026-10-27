#!/bin/bash
set -e
build() {
  docker buildx build . --platform=linux/amd64,linux/arm64 --provenance=false -f $1.Dockerfile -t iceshrimp.dev/iceshrimp/$2 --push --pull
}

build dotnet-sdk-10.0-alpine       dotnet-sdk:10.0-alpine
build dotnet-sdk-10.0-alpine-wasm  dotnet-sdk:10.0-alpine-wasm
build dotnet-sdk-10.0-wasm         dotnet-sdk:10.0-wasm
build dotnet-sdk-11.0-alpine       dotnet-sdk:11.0-alpine
build dotnet-sdk-11.0-alpine-wasm  dotnet-sdk:11.0-alpine-wasm
build dotnet-sdk-11.0-wasm         dotnet-sdk:11.0-wasm

build ci-env-dotnet10       ci-env:dotnet10
build ci-env-dotnet10-wasm  ci-env:dotnet10-wasm
build ci-env-dotnet11       ci-env:dotnet11
build ci-env-dotnet11-wasm  ci-env:dotnet11-wasm

docker buildx prune -a -f --keep-storage 10G
