# syntax=docker/dockerfile:1

# ---------- 1. Bibliotecas front-end (npm) ----------
FROM node:25-alpine AS client
WORKDIR /client
COPY src/MindCare.Web/package.json src/MindCare.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/MindCare.Web/scripts ./scripts
RUN npm run vendor

# ---------- 2. Build e publish (.NET) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura dependências em uma camada separada (cache do Docker)
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/MindCare.Application/MindCare.Application.csproj src/MindCare.Application/
COPY src/MindCare.Web/MindCare.Web.csproj src/MindCare.Web/
RUN dotnet restore src/MindCare.Web/MindCare.Web.csproj

COPY src/ src/
COPY --from=client /client/wwwroot/lib src/MindCare.Web/wwwroot/lib
RUN dotnet publish src/MindCare.Web/MindCare.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    -p:SkipClientLibraries=true \
    -p:UseAppHost=false

# ---------- 3. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=America/Sao_Paulo

COPY --from=build /app .

# Usuário sem privilégios fornecido pela imagem oficial
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "MindCare.Web.dll"]
