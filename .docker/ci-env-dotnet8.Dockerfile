FROM iceshrimp.dev/iceshrimp/dotnet-sdk:8.0-alpine
RUN apk add --no-cache --no-progress git docker-cli docker-cli-buildx python3 curl go nodejs-current tar zstd make
CMD ["/bin/bash"]
