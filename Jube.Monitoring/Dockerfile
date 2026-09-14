FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Jube.Monitoring/Jube.Monitoring.csproj", "Jube.Monitoring/"]
COPY ["Jube.Data/Jube.Data.csproj", "Jube.Data/"]
COPY ["Accord.Statistics/Accord.Statistics.csproj", "Accord.Statistics/"]
COPY ["Accord.Math/Accord.Math.csproj", "Accord.Math/"]
COPY ["Accord.Math.Core/Accord.Math.Core.csproj", "Accord.Math.Core/"]
COPY ["Accord.Core/Accord.Core.csproj", "Accord.Core/"]
COPY ["Jube.Extensions/Jube.Extensions.csproj", "Jube.Extensions/"]
COPY ["Jube.Dictionary/Jube.Dictionary.csproj", "Jube.Dictionary/"]
COPY ["Jube.TaskCancellation/Jube.TaskCancellation.csproj", "Jube.TaskCancellation/"]
COPY ["Jube.ResilientNpgsql/Jube.ResilientNpgsql.csproj", "Jube.ResilientNpgsql/"]

RUN dotnet restore "Jube.Monitoring/Jube.Monitoring.csproj"
COPY . .
WORKDIR "/src/Jube.Monitoring"
RUN dotnet build "Jube.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Jube.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Jube.Monitoring.dll"]
