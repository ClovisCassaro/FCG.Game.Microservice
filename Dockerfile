# ==========================================
# Stage 1: Base Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# ==========================================
# Stage 2: Build
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todos os arquivos .csproj
COPY ["FCG.Game.API/FCG.Game.API.csproj", "FCG.Game.API/"]
COPY ["FCG.Game.Application/FCG.Game.Application.csproj", "FCG.Game.Application/"]
COPY ["FCG.Game.Domain/FCG.Game.Domain.csproj", "FCG.Game.Domain/"]
COPY ["FCG.Game.Infrastructure/FCG.Game.Infrastructure.csproj", "FCG.Game.Infrastructure/"]

# Restore
RUN dotnet restore "FCG.Game.API/FCG.Game.API.csproj"

# Copiar todo o código
COPY . .

# Build
WORKDIR "/src/FCG.Game.API"
RUN dotnet build "FCG.Game.API.csproj" -c Release -o /app/build

# ==========================================
# Stage 3: Publish
# ==========================================
FROM build AS publish
RUN dotnet publish "FCG.Game.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Stage 4: Final
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl --fail http://localhost:80/health || exit 1

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FCG.Game.API.dll"]