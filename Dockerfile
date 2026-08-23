# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files first for better Docker layer caching
COPY EduMcq.API/EduMcq.API.csproj EduMcq.API/
COPY EduMcq.Common/EduMcq.Common.csproj EduMcq.Common/
COPY EduMcq.Interfaces/EduMcq.Interfaces.csproj EduMcq.Interfaces/
COPY EduMcq.Models/EduMcq.Models.csproj EduMcq.Models/
COPY EduMcq.Repository/EduMcq.Repository.csproj EduMcq.Repository/
COPY EduMcq.Services/EduMcq.Services.csproj EduMcq.Services/

RUN dotnet restore EduMcq.API/EduMcq.API.csproj

# Copy source
COPY . .

# Publish only the API project
RUN dotnet publish EduMcq.API/EduMcq.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EduMcq.API.dll"]