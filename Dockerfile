FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder
ARG TARGETARCH
WORKDIR /src

# copy csproj/fsproj and restore as distinct layers
RUN mkdir Iceshrimp.Backend Iceshrimp.Parsing Iceshrimp.Frontend
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing
COPY Iceshrimp.Frontend/*.csproj /src/Iceshrimp.Frontend
WORKDIR /src/Iceshrimp.Backend
RUN dotnet restore -a $TARGETARCH

# copy build files
COPY Iceshrimp.Backend/. /src/Iceshrimp.Backend
COPY Iceshrimp.Parsing/. /src/Iceshrimp.Parsing
COPY Iceshrimp.Frontend/. /src/Iceshrimp.Frontend

# build
RUN dotnet publish --no-restore -a $TARGETARCH -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-composite
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
