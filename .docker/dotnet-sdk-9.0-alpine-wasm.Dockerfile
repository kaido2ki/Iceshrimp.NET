FROM mcr.microsoft.com/dotnet/sdk:9.0-preview-alpine
RUN dotnet workload install wasm-tools
