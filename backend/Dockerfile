# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore BasicMakerspaceDigitalTwinBackend.sln
RUN dotnet publish src/DigitalTwin.Api/DigitalTwin.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# IMPORTANT: force port 5017
ENV ASPNETCORE_URLS=http://+:5017
EXPOSE 5017

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DigitalTwin.Api.dll"]