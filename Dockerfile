# syntax=docker/dockerfile-upstream:master
# To build with ILLink & AOT enabled, run docker build --build-arg="AOT=true"
# To build without VIPS support, run docker build --build-arg="VIPS=false"

ARG AOT=false

ARG IMAGE=${AOT/true/alpine-wasm}
ARG IMAGE=${IMAGE/false/alpine}

FROM --platform=$BUILDPLATFORM iceshrimp.dev/iceshrimp/dotnet-sdk:9.0-$IMAGE AS builder
WORKDIR /src
ARG BUILDPLATFORM
ARG AOT=false

# copy csproj/fsproj & nuget config, then restore as distinct layers
COPY NuGet.Config /src
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/*.csproj /src/Iceshrimp.Shared/
COPY Iceshrimp.Build/*.csproj /src/Iceshrimp.Build/
COPY Iceshrimp.Build/*.props /src/Iceshrimp.Build/
COPY Directory.Build.props /src/Directory.Build.props

WORKDIR /src/Iceshrimp.Backend
ARG VIPS=true
RUN --mount=type=cache,target=/root/.nuget \
    dotnet restore -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# copy build files
COPY Iceshrimp.Backend/ /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/ /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/ /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/ /src/Iceshrimp.Shared/

# copy files required for sourcelink
COPY .git/HEAD /src/.git/HEAD
COPY .git/config /src/.git/config
COPY .git/refs/heads/ /src/.git/refs/heads/
RUN mkdir -p /src/.git/objects

# build without architecture set, allowing for reuse of the majority of the compiled IL between architectures
RUN --mount=type=cache,target=/root/.nuget \
    dotnet publish --no-restore -c Release -o /build -p:EnableAOT=$AOT -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS -p:DeterministicSourcePaths=true -p:ContinuousIntegrationBuild=true

# if architecture doesn't match, build with architecture set, otherwise use existing compile output
ARG TARGETPLATFORM
ARG TARGETARCH

RUN --mount=type=cache,target=/root/.nuget \
    if [[ "$BUILDPLATFORM" != "$TARGETPLATFORM" ]]; then \
        dotnet restore -a $TARGETARCH -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS; \
        dotnet publish --no-restore -c Release -a $TARGETARCH -o /app-$TARGETARCH -p:EnableAOT=$AOT -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS -p:DeterministicSourcePaths=true -p:ContinuousIntegrationBuild=true; \
        mv /app-$TARGETARCH /app; else mv /build /app; \
    fi

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine-composite AS image
WORKDIR /app
COPY --from=builder /app .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
