# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# SDK para compilar a aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copia TUDO do seu repositório para a pasta /src do container
COPY . .

# 2. Restaura as dependências apontando para o projeto principal
RUN dotnet restore "src/TruckFlowApi/TruckFlow/TruckFlow.csproj"

# 3. Faz o Build e o Publish de uma vez só
RUN dotnet publish "src/TruckFlowApi/TruckFlow/TruckFlow.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Imagem final (leve) apenas com o que foi compilado
FROM base AS final
WORKDIR /app

# Copia os arquivos gerados no passo 3 para a imagem final
COPY --from=build /app/publish .

# Garante que o Railway consiga injetar a porta corretamente
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TruckFlow.dll"]