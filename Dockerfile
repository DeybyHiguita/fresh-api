# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solución y archivos de proyecto
COPY Fresh.sln .
COPY Fresh.Core/Fresh.Core.csproj Fresh.Core/
COPY Fresh.Infrastructure/Fresh.Infrastructure.csproj Fresh.Infrastructure/
COPY Fresh.Api/Fresh.Api.csproj Fresh.Api/
RUN dotnet restore

# Copiar código fuente y publicar
COPY . .
RUN dotnet publish Fresh.Api/Fresh.Api.csproj -c Release -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Fresh.Api.dll"]
