# TODO: build frontend

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend-builder
WORKDIR /source

# copy csproj and restore as distinct layers
COPY Iceshrimp.Backend/*.csproj .
RUN dotnet restore -a amd64 #TODO: make this configurable but defaulting to the current architecture

# copy and publish app and libraries
COPY Iceshrimp.Backend/. .
RUN dotnet publish --no-restore -a amd64 -o /app #TODO: make this configurable but defaulting to the current architecture

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./Iceshrimp.Backend"]
