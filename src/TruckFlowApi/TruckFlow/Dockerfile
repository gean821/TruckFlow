# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar todos os projetos da solution
COPY ["src/TruckFlowApi/TruckFlow/TruckFlow.csproj", "src/TruckFlowApi/TruckFlow/"]
COPY ["src/TruckFlow.Application/TruckFlow.Application.csproj", "src/TruckFlow.Application/"]
COPY ["src/TruckFlow.Domain/TruckFlow.Domain.csproj", "src/TruckFlow.Domain/"]
COPY ["src/TruckFlowApi.Infra/TruckFlowApi.Infra.csproj", "src/TruckFlowApi.Infra/"]

# Restaurar dependências
RUN dotnet restore "src/TruckFlowApi/TruckFlow/TruckFlow.csproj"

# Copiar todo o código
COPY . .

WORKDIR "/src/src/TruckFlowApi/TruckFlow"
RUN dotnet build "TruckFlow.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TruckFlow.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TruckFlow.dll"]