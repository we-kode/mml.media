FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt update && apt install ffmpeg -y
ARG USERNAME=mml
ARG UID
ARG GID
RUN groupadd --gid $GID $USERNAME \
    && useradd --uid $UID --gid $GID -m $USERNAME
USER $USERNAME
RUN mkdir -p /records_tmp
WORKDIR /app
EXPOSE 5052
ENV ASPNETCORE_ENVIRONMENT=Release

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR /src
COPY ["Media.API/Media.API.csproj", "Media.API/"]
COPY ["Media.DBContext/Media.DBContext.csproj", "Media.DBContext/"]
RUN dotnet restore "Media.API/Media.API.csproj"
COPY . .
WORKDIR "/src/Media.API"
RUN dotnet build "Media.API.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/media

FROM build AS publish
ARG DOT_NET_BUILD_CONFIG=Release
RUN dotnet publish "Media.API.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/media

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "media/Media.API.dll"]