# Base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copia o repositório inteiro para o container
COPY . .

# 2. GOLPE DE MESTRE: Limpa as pastas obj e bin que vieram do Windows e quebram o Linux
RUN find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + || true

# 3. Restaura as dependências (focando apenas na API e nos projetos que ela usa)
RUN dotnet restore "src/TruckFlowApi/TruckFlow/TruckFlow.csproj"

# 4. Faz o build e publish
RUN dotnet publish "src/TruckFlowApi/TruckFlow/TruckFlow.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TruckFlow.dll"]