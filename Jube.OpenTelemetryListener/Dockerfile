FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER $APP_UID
WORKDIR /app
EXPOSE 4318

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Jube.OpenTelemetryListener/Jube.OpenTelemetryListener.csproj", "Jube.OpenTelemetryListener/"]

RUN dotnet restore "Jube.OpenTelemetryListener/Jube.OpenTelemetryListener.csproj"
COPY . .
WORKDIR "/src/Jube.OpenTelemetryListener"
RUN dotnet build "Jube.OpenTelemetryListener.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Jube.OpenTelemetryListener.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Jube.OpenTelemetryListener.dll"]
