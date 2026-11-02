# Fase base para a execução da aplicação
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Fase para compilar o projeto de serviço e o frontend Angular
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs && \
    npm install -g @angular/cli

WORKDIR /src
COPY ["Consultorio.Server/Consultorio.Server.csproj", "Consultorio.Server/"]
COPY ["Consultorio.Dominio/Consultorio.Dominio.csproj", "Consultorio.Dominio/"]
COPY ["Consultorio.Aplicacao/Consultorio.Aplicacao.csproj", "Consultorio.Aplicacao/"]
COPY ["Consultorio.Infraestrutura/Consultorio.Infraestrutura.csproj", "Consultorio.Infraestrutura/"]
COPY ["Consultorio.Autenticacao.Modelo/Consultorio.Identity.Modelo.csproj", "Consultorio.Autenticacao.Modelo/"]
COPY ["Consultorio.Identity.Infraestrutura/Consultorio.Identity.Infraestrutura.csproj", "Consultorio.Identity.Infraestrutura/"]
COPY ["Consultorio.Cliente/package*.json", "Consultorio.Cliente/"]
RUN dotnet restore "./Consultorio.Server/Consultorio.Server.csproj"

COPY . .

# Construir o frontend Angular
WORKDIR /src/Consultorio.Cliente
RUN npm install
RUN npm run build

# Construir e publicar o projeto ASP.NET
WORKDIR /src/Consultorio.Server
RUN dotnet build "./Consultorio.Server.csproj" -c Release -o /app/build
RUN dotnet publish "./Consultorio.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Fase final para a produção
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Consultorio.Server.dll"]
