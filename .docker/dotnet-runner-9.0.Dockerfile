FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine-composite
ARG TARGETARCH
WORKDIR /app
COPY linux-musl-$TARGETARCH/ .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
