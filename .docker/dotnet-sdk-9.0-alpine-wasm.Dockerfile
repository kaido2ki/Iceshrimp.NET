FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
RUN dotnet workload install wasm-tools
RUN apk add --no-cache --no-progress bash python3
RUN ln -sf /bin/bash /bin/sh
CMD ["/bin/bash"]
