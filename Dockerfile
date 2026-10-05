# Etapa 1: Build y compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar archivos de proyectos para aprovechar el cache de capas de Docker
COPY TenisAhora.slnx ./
COPY src/TenisAhora.Domain/*.csproj ./src/TenisAhora.Domain/
COPY src/TenisAhora.Application/*.csproj ./src/TenisAhora.Application/
COPY src/TenisAhora.Infrastructure/*.csproj ./src/TenisAhora.Infrastructure/
COPY src/TenisAhora.API/*.csproj ./src/TenisAhora.API/

# Restaurar dependencias de la solución
RUN dotnet restore src/TenisAhora.API/TenisAhora.API.csproj

# Copiar el código fuente completo y compilar en Release
COPY src/ ./src/
RUN dotnet publish src/TenisAhora.API/TenisAhora.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime final optimizado y liviano
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiar los artefactos compilados desde la etapa anterior
COPY --from=build /app/publish .

# Puerto por defecto para ASP.NET Core en contenedores modernos (8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TenisAhora.API.dll"]
