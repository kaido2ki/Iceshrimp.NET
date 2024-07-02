FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
RUN apk add --no-cache --no-progress git docker bash python3 curl go nodejs-current zstd
RUN curl -o /bin/git-restore-mtime https://raw.githubusercontent.com/MestreLion/git-tools/main/git-restore-mtime
RUN chmod +x /bin/git-restore-mtime
CMD ["/bin/sh"]
