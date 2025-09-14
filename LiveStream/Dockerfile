FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
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
ENTRYPOINT ["dotnet", "LiveStream.dll", "--m2tcp", "--m2tcphost", "221.249.141.10", "--m2tcpport", "13994", "--m2tcpconn", "50", "--m2tcpreset", "--sinkhttp", "--sinkhttpport", "61666", "--sinkbuffer", "250"]
