#FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
FROM mcr.microsoft.com/dotnet/runtime:6.0.11-bullseye-slim-arm32v7 AS base

WORKDIR /app

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
#FROM mcr.microsoft.com/dotnet/runtime:6.0.11-bullseye-slim-arm32v7 AS build
WORKDIR /src
COPY ["HabraBot.csproj", "./"]
RUN dotnet restore "HabraBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "HabraBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HabraBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app 
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HabraBot.dll"]
