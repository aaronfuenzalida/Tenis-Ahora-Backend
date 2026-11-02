FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para que el restore quede cacheado entre builds
COPY src/TenisAhora.API/TenisAhora.API.csproj src/TenisAhora.API/
COPY src/TenisAhora.Application/TenisAhora.Application.csproj src/TenisAhora.Application/
COPY src/TenisAhora.Domain/TenisAhora.Domain.csproj src/TenisAhora.Domain/
COPY src/TenisAhora.Infrastructure/TenisAhora.Infrastructure.csproj src/TenisAhora.Infrastructure/
RUN dotnet restore src/TenisAhora.API/TenisAhora.API.csproj

COPY src/ src/
RUN dotnet publish src/TenisAhora.API/TenisAhora.API.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
# Los recibos y los plazos de reserva usan la hora local del club.
ENV TZ=America/Argentina/Buenos_Aires PORT=5090 DOTNET_EnableDiagnostics=0
# Mismo uid que el chown del deploy sobre el volumen de datos (/app/data).
USER 1000:1000
EXPOSE 5090
ENTRYPOINT ["dotnet", "TenisAhora.API.dll"]
