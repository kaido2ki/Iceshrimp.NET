#!/bin/bash
set -e
build() {
  docker buildx build . --platform=linux/amd64,linux/arm64 --provenance=false -f $1.Dockerfile -t iceshrimp.dev/iceshrimp/$2 --push --pull
}

build dotnet-sdk-8.0-alpine      dotnet-sdk:8.0-alpine
build dotnet-sdk-8.0-wasm        dotnet-sdk:8.0-wasm
build dotnet-sdk-9.0-alpine      dotnet-sdk:9.0-alpine
build dotnet-sdk-9.0-alpine-wasm dotnet-sdk:9.0-alpine-wasm

build ci-env       ci-env:dotnet
build ci-env-wasm  ci-env:dotnet-wasm

docker buildx prune -a -f --keep-storage 10G
