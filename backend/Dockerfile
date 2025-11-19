# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY PortfolioBalance/PortfolioBalance.csproj PortfolioBalance/
RUN dotnet restore PortfolioBalance/PortfolioBalance.csproj

# Copy everything else and build
COPY PortfolioBalance/ PortfolioBalance/
WORKDIR /src/PortfolioBalance
RUN dotnet build PortfolioBalance.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish PortfolioBalance.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PortfolioBalance.dll"]
