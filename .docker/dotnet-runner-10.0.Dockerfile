FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-composite
RUN apk add --no-cache --no-progress ca-certificates
ARG TARGETARCH
WORKDIR /app
COPY linux-musl-$TARGETARCH/ .
USER app
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
