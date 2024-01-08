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
WORKDIR /backend

# copy csproj and restore as distinct layers
COPY Iceshrimp.Backend/*.csproj .
RUN dotnet restore -a $TARGETARCH

# copy and build backend
COPY Iceshrimp.Backend/. .
RUN dotnet publish --no-restore -a $TARGETARCH -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-composite
WORKDIR /app
COPY --from=backend /app .
COPY --from=frontend /frontend/dist ./wwwroot
ENTRYPOINT ["./Iceshrimp.Backend"]
