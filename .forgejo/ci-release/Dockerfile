FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS image-aot
ARG TARGETARCH
WORKDIR /app
COPY linux-$TARGETARCH/ .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
