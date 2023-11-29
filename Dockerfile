FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt update && apt upgrade
RUN apt install build-essential -y
RUN apt install curl -y
RUN apt install ffmpeg -y
RUN ffmpeg -version

WORKDIR /libsodium
RUN curl https://download.libsodium.org/libsodium/releases/LATEST.tar.gz | tar zxf -
WORKDIR /libsodium/libsodium-stable
RUN ./configure && make && make check && make install

WORKDIR /libopus
RUN curl -L https://downloads.xiph.org/releases/opus/opus-1.4.tar.gz | tar zxf -
WORKDIR /libopus/opus-1.4
RUN ./configure && make && make check && make install

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