# Fase base para a execução da aplicação no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Fase para compilar o projeto de serviço e o frontend Angular
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update
RUN apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get install -y nodejs
RUN npm install -g @angular/cli

WORKDIR /src
COPY ["Consultorio.Server/Consultorio.Server.csproj", "Consultorio.Server/"]
COPY ["consultorio.client/consultorio.client.csproj", "consultorio.client/"]
RUN dotnet restore "./Consultorio.Server/Consultorio.Server.csproj"
COPY . .

# Construir o frontend Angular
WORKDIR /src/consultorio.client
RUN npm install
RUN ng build

# Criar a pasta wwwroot no projeto ASP.NET se não existir
RUN mkdir -p /src/Consultorio.Server/wwwroot

# Copiar os arquivos construídos do Angular para o diretório wwwroot do ASP.NET
WORKDIR /src/Consultorio.Server
RUN cp -r /src/consultorio.client/dist/* ./wwwroot/

# Construir e publicar o projeto ASP.NET
RUN dotnet build "./Consultorio.Server.csproj" -c Release -o /app/build
RUN dotnet publish "./Consultorio.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Fase final para a produção
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Consultorio.Server.dll"]
