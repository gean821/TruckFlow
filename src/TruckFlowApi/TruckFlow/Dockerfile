# Base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copia o repositório inteiro (a partir da raiz) para dentro do container
COPY . .

# 2. Restaura as dependências pela Solution (assim ele mapeia Application, Domain, Infra, etc.)
RUN dotnet restore "src/TruckFlowApi/TruckFlowApi.sln"

# 3. Faz o build e publish apontando para o projeto principal
RUN dotnet publish "src/TruckFlowApi/TruckFlow/TruckFlow.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TruckFlow.dll"]