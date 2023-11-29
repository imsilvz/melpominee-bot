FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt update && apt upgrade
RUN apt install curl -y
RUN apt install ffmpeg -y
RUN ffmpeg -version

WORKDIR /libsodium
RUN curl https://download.libsodium.org/libsodium/releases/LATEST.tar.gz | tar zxf -
RUN ./configure && make && make check

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