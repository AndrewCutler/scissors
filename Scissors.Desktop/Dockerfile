FROM mcr.microsoft.com/dotnet/sdk:10.0-jammy AS build
WORKDIR /src

COPY Scissors.Desktop/Scissors.Desktop.csproj Scissors.Desktop/
RUN dotnet restore Scissors.Desktop/Scissors.Desktop.csproj

COPY . .
RUN dotnet publish Scissors.Desktop/Scissors.Desktop.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-jammy
ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        fluxbox \
        libasound2 \
        libcairo2 \
        libfontconfig1 \
        libfreetype6 \
        libgdk-pixbuf-2.0-0 \
        libglib2.0-0 \
        libgtk-3-0 \
        libpango-1.0-0 \
        libx11-6 \
        libxcursor1 \
        libxext6 \
        libxinerama1 \
        libxi6 \
        libxrandr2 \
        libxrender1 \
        libxss1 \
        x11-utils \
        x11vnc \
        xvfb \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY Scissors.Desktop/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

ENV DISPLAY=:99 \
    XDG_RUNTIME_DIR=/tmp/runtime-root

EXPOSE 5900

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
