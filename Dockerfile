# ============================================
# Stage 1: Build Angular client
# ============================================
FROM node:22-slim AS angular-build

WORKDIR /app
COPY InvoicerClient/package*.json ./
RUN npm ci
COPY InvoicerClient/ ./
RUN npm run build

# ============================================
# Stage 2: Build .NET API
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Invoicer/Invoicer.csproj Invoicer/
RUN dotnet restore Invoicer/Invoicer.csproj
COPY Invoicer/ Invoicer/
WORKDIR /src/Invoicer
RUN dotnet publish Invoicer.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Final runtime image
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=dotnet-build /app/publish .
COPY --from=angular-build /app/dist/InvoicerClient/browser ./wwwroot

EXPOSE 8080
ENTRYPOINT ["dotnet", "Invoicer.dll"]
