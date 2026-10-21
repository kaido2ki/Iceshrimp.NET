FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine
RUN apk add --no-cache --no-progress bash ca-certificates
RUN ln -sf /bin/bash /bin/sh
CMD ["/bin/bash"]
