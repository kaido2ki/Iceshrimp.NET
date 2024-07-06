FROM iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-alpine
RUN apk add --no-cache --no-progress git docker python3 curl go nodejs-current tar zstd
CMD ["/bin/bash"]
