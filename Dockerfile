FROM --platform=$BUILDPLATFORM alpine:3.18 AS frontend
WORKDIR /frontend

RUN apk add --no-cache --no-progress nodejs-current npm

# copy frontend package.json and yarn.lock
COPY Iceshrimp.Frontend/package.json Iceshrimp.Frontend/yarn.lock .

# Configure corepack and install dev mode dependencies for compilation
RUN corepack enable && corepack prepare --activate && yarn --immutable

# copy and build frontend
COPY Iceshrimp.Frontend/. .
RUN yarn --immutable && yarn build
#TODO: why is a second yarn pass required here?

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend
ARG TARGETARCH
WORKDIR /src

# copy csproj and restore as distinct layers
RUN mkdir Iceshrimp.Backend Iceshrimp.Parsing
COPY Iceshrimp.Backend/*.csproj /src/Iceshrimp.Backend
COPY Iceshrimp.Parsing/*.fsproj /src/Iceshrimp.Parsing
WORKDIR /src/Iceshrimp.Backend
RUN dotnet restore -a $TARGETARCH

# copy backend files
COPY Iceshrimp.Backend/. /src/Iceshrimp.Backend
COPY Iceshrimp.Parsing/. /src/Iceshrimp.Parsing

# it's faster if we copy this later because we can parallelize it with buildkit, but the build fails if this file doesn't exist
RUN mkdir -p ./wwwroot/.vite/ && touch ./wwwroot/.vite/manifest.json

# build backend
RUN dotnet publish --no-restore -a $TARGETARCH -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-composite
WORKDIR /app
COPY --from=backend /app .
COPY --from=frontend /frontend/dist ./wwwroot
ENTRYPOINT ["./Iceshrimp.Backend", "--environment", "Production", "--migrate-and-start"]
