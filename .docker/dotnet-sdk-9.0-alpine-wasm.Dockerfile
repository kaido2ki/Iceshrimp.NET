FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
RUN dotnet workload install wasm-tools
RUN apk add --no-cache --no-progress bash python3

# Workaround for https://github.com/dotnet/sdk/issues/44933
RUN apk add --no-cache --no-progress go
RUN go install github.com/yaegashi/muslstack@latest
RUN find /usr/share/dotnet/packs -name wasm-opt -type f | xargs ~/go/bin/muslstack -s 0x800000
RUN rm -rf ~/go
RUN apk del --no-cache --no-progress go

RUN ln -sf /bin/bash /bin/sh
CMD ["/bin/bash"]
