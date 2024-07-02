FROM mcr.microsoft.com/dotnet/sdk:8.0
RUN apt-get update && apt-get install git bash python3 curl golang nodejs zstd ca-certificates -y
RUN install -m 0755 -d /etc/apt/keyrings
RUN curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
RUN chmod a+r /etc/apt/keyrings/docker.asc
RUN echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
RUN apt-get update && apt-get install docker-ce docker-ce-cli containerd.io docker-buildx-plugin -y
RUN curl -o /bin/git-restore-mtime https://raw.githubusercontent.com/MestreLion/git-tools/main/git-restore-mtime
RUN chmod +x /bin/git-restore-mtime
RUN dotnet workload install wasm-tools
CMD ["/bin/sh"]
