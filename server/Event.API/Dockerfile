FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy csproj files and restore as distinct layers
COPY ["Event.API/Event.API.csproj", "Event.API/"]
COPY ["Event.Business/Event.Business.csproj", "Event.Business/"]
COPY ["Event.Data/Event.Data.csproj", "Event.Data/"]
COPY ["Event.Contracts/Event.Contracts.csproj", "Event.Contracts/"]
COPY ["Event.Models/Event.Models.csproj", "Event.Models/"]

RUN dotnet restore "Event.API/Event.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Event.API"
RUN dotnet build "Event.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Event.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Event.API.dll"]
