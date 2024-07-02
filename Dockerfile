# To build with ILLink & AOT enabled, run docker build --target image-aot
# To build without VIPS support, run docker build --build-arg="VIPS=false"

# We have to build & run AOT images on linux-glibc, at least until .NET 9.0 (See https://github.com/dotnet/sdk/issues/32327 for details)

FROM --platform=$BUILDPLATFORM iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-alpine AS builder-jit
ARG BUILDPLATFORM
WORKDIR /src

# copy csproj/fsproj & nuget config, then restore as distinct layers
COPY NuGet.Config /src
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/*.csproj /src/Iceshrimp.Shared/

WORKDIR /src/Iceshrimp.Backend
ARG VIPS=true
RUN --mount=type=cache,target=/root/.nuget \
    dotnet restore -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# copy build files
COPY Iceshrimp.Backend/ /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/ /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/ /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/ /src/Iceshrimp.Shared/

# build without architecture set, allowing for reuse of the majority of the compiled IL between architectures
RUN --mount=type=cache,target=/root/.nuget \
    dotnet publish --no-restore -c Release -o /build -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# if architecture doesn't match, build with architecture set, otherwise use existing compile output
ARG TARGETPLATFORM
ARG TARGETARCH

RUN --mount=type=cache,target=/root/.nuget \
    if [[ "$BUILDPLATFORM" != "$TARGETPLATFORM" ]]; then \
        dotnet restore -a $TARGETARCH -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS; \
        dotnet publish --no-restore -c Release -a $TARGETARCH -o /app-$TARGETARCH -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS; \
        mv /app-$TARGETARCH /app; else mv /build /app; \
    fi

FROM --platform=$BUILDPLATFORM iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-wasm AS builder-aot
ARG BUILDPLATFORM
WORKDIR /src

# copy csproj/fsproj & nuget config, then restore as distinct layers
COPY NuGet.Config /src
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/*.csproj /src/Iceshrimp.Shared/

WORKDIR /src/Iceshrimp.Backend
ARG VIPS=true
RUN --mount=type=cache,target=/root/.nuget \
    dotnet restore -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# copy build files
COPY Iceshrimp.Backend/ /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/ /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/ /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/ /src/Iceshrimp.Shared/

# build without architecture set, allowing for reuse of the majority of the compiled IL between architectures
RUN --mount=type=cache,target=/root/.nuget \
    dotnet publish --no-restore -c Release -o /build -p:EnableAOT=true -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# if architecture doesn't match, build with architecture set, otherwise use existing compile output
ARG TARGETPLATFORM
ARG TARGETARCH

RUN --mount=type=cache,target=/root/.nuget \
    if [[ "$BUILDPLATFORM" != "$TARGETPLATFORM" ]]; then \
        dotnet restore -a $TARGETARCH -p:Configuration=Release -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS; \
        dotnet publish --no-restore -c Release -a $TARGETARCH -o /app-$TARGETARCH -p:EnableAOT=true -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS; \
        mv /app-$TARGETARCH /app; else mv /build /app; \
    fi

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS image-aot
WORKDIR /app
COPY --from=builder-aot /app .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-composite AS image-jit
WORKDIR /app
COPY --from=builder-jit /app .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
