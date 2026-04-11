# Base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copia tudo a partir da raiz
COPY . .

# limpa lixo do windows (bin/obj) que quebra o build no Linux
RUN find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + || true

# caminho CORRETO do csproj
RUN dotnet restore "src/TruckFlowApi/TruckFlow/TruckFlow.csproj"

# build
RUN dotnet publish "src/TruckFlowApi/TruckFlow/TruckFlow.csproj" -c Release -o /app/publish /p:UseAppHost=false

# runtime
FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TruckFlow.dll"]