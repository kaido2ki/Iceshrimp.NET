#!/bin/bash
set -e
docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f dotnet-sdk-8.0-alpine.Dockerfile -t iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-alpine --push
docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f dotnet-sdk-8.0-wasm.Dockerfile -t iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-wasm --push

docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f dotnet-sdk-9.0-alpine.Dockerfile -t iceshrimp.dev/iceshrimp/dotnet-sdk:9.0-alpine --push
docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f dotnet-sdk-9.0-alpine-wasm.Dockerfile -t iceshrimp.dev/iceshrimp/dotnet-sdk:9.0-alpine-wasm --push

docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f ci-env.Dockerfile -t iceshrimp.dev/iceshrimp/ci-env:dotnet --push
docker buildx build . --builder build-cluster --platform=linux/amd64,linux/arm64 --provenance=false -f ci-env-wasm.Dockerfile -t iceshrimp.dev/iceshrimp/ci-env:dotnet-wasm --push

docker buildx prune -a -f --builder build-cluster
