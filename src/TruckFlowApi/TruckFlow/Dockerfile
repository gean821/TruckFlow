# Base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + || true

RUN dotnet restore "src/TruckFlowApi/TruckFlow/TruckFlow.csproj"
RUN dotnet publish "src/TruckFlowApi/TruckFlow/TruckFlow.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TruckFlow.dll"]