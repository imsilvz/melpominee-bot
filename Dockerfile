FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt update && apt upgrade -y
RUN apt install build-essential chrpath libssl-dev libxft-dev \
				python3-launchpadlib software-properties-common wget \
				libfreetype6 libfreetype6-dev libfontconfig1 libfontconfig1-dev \
				bzip2 bzip2-libs -y

RUN wget https://bitbucket.org/ariya/phantomjs/downloads/phantomjs-2.1.1-linux-x86_64.tar.bz2 -O /tmp/phantomjs.tar.bz2
RUN tar xvjf /tmp/phantomjs.tar.bz2
RUN mv /tmp/phantomjs-2.1.1-linux-x86_64 /usr/local/share
RUN ln -sf /usr/local/share/phantomjs-2.1.1-linux-x86_64/bin/phantomjs /usr/local/bin

RUN wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp
RUN yt-dlp -U

RUN apt install ffmpeg libopus0 libopus-dev libsodium23 libsodium-dev -y
RUN ffmpeg -version
RUN yt-dlp --version

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . /src
WORKDIR /src
RUN ls
RUN dotnet build "Melpominee.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Melpominee.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Melpominee.dll"]