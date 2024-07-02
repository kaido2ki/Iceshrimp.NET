FROM mcr.microsoft.com/dotnet/sdk:8.0
RUN --mount=type=cache,target=/root/.nuget dotnet workload install wasm-tools
RUN apt-get update && apt-get install python3 -y
CMD ["/bin/sh"]
