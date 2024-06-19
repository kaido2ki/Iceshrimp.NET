# To build with ILLink & AOT enabled, run docker build --target image-aot
# To build without VIPS support, run docker build --build-arg="VIPS=false"

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder-jit
WORKDIR /src
# copy csproj/fsproj & nuget config, then restore as distinct layers
COPY NuGet.Config /src
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/*.csproj /src/Iceshrimp.Shared/
WORKDIR /src/Iceshrimp.Backend
ARG VIPS=true
ARG TARGETARCH
RUN dotnet restore -a $TARGETARCH -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# copy build files
COPY . /src/

# build
RUN dotnet publish --no-restore -c Release -a $TARGETARCH -o /app -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS builder-aot
RUN dotnet workload install wasm-tools
RUN apt-get update && apt-get install python3 -y
WORKDIR /src

# copy csproj/fsproj & nuget config, then restore as distinct layers
COPY NuGet.Config /src
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend/
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing/
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend/
COPY Iceshrimp.Shared/*.csproj /src/Iceshrimp.Shared/
WORKDIR /src/Iceshrimp.Backend
ARG VIPS=true
ARG TARGETARCH
RUN dotnet restore -a $TARGETARCH -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

# copy build files
COPY . /src/

# build
RUN dotnet publish --no-restore -c Release -a $TARGETARCH -o /app -p:EnableAOT=true -p:BundleNativeDeps=$VIPS -p:EnableLibVips=$VIPS

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
