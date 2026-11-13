FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt update && apt install -y ffmpeg libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
#ARG USERNAME=mml
ARG UID
ARG GID
#RUN groupadd --gid $GID $USERNAME \
#    && useradd --uid $UID --gid $GID -m $USERNAME
#USER $USERNAME
RUN mkdir /records
RUN chown -R $USERNAME:$USERNAME /records
RUN chmod 755 /records
VOLUME ["/records"]

USER $USERNAME
WORKDIR /app
EXPOSE 5052
ENV ASPNETCORE_ENVIRONMENT=Release

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR /src
COPY ["Media.API/Media.API.csproj", "Media.API/"]
COPY ["Media.DBContext/Media.DBContext.csproj", "Media.DBContext/"]
COPY ["Media.Application/Media.Application.csproj", "Media.Application/"]
COPY ["Media.Infrastructure/Media.Infrastructure.csproj", "Media.Infrastructure/"]
RUN dotnet restore "Media.API/Media.API.csproj"
COPY . .
WORKDIR "/src/Media.API"
RUN dotnet build "Media.API.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/media

FROM build AS publish
ARG DOT_NET_BUILD_CONFIG=Release
RUN dotnet publish "Media.API.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/media

# Third-party notices of all NuGet packages. Licenses of system packages like ffmpeg are in /usr/share/doc.
FROM build AS licenses
WORKDIR /src
RUN dotnet tool install nuget-license --version 4.0.18 --tool-path /tools
RUN /tools/nuget-license -i Media.API/Media.API.csproj -t -ignore WeKoDe.MML.Messages -o Markdown     -fo /licenses/THIRD-PARTY-NOTICES.md -d /licenses/texts

FROM base AS final
WORKDIR /app
RUN mkdir -p /secrets
COPY --from=publish /app/publish .
COPY --from=licenses /licenses ./licenses
ENTRYPOINT ["dotnet", "media/Media.API.dll"]
