FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt update && apt upgrade -y
RUN apt install python3-launchpadlib software-properties-common wget -y

RUN wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O ~/.local/bin/yt-dlp
RUN chmod a+rx ~/.local/bin/yt-dlp  # Make executable
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