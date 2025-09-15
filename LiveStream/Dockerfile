FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
ADD --link https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb /
RUN dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb; \
  apt-get update && apt-get install -y libmsquic; \
  apt clean -y && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LiveStream/LiveStream.csproj", "LiveStream/"]
RUN dotnet restore "LiveStream/LiveStream.csproj"
COPY . .
WORKDIR "/src/LiveStream"
RUN dotnet build "LiveStream.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LiveStream.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveStream.dll", "--quic", "--quichost", "221.249.141.10", "--quicport", "14040", "--sinkhttp", "--sinkhttpport", "61666", "--sinkbuffer", "50"]
