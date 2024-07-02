FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
RUN apk add --no-cache --no-progress git docker bash python3 curl go nodejs-current zstd
CMD ["/bin/sh"]
