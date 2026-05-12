FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Itm.Inventory.Api/Itm.Inventory.Api.csproj"
WORKDIR "/src/Itm.Inventory.Api"
RUN dotnet publish "Itm.Inventory.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Itm.Inventory.Api.dll"]
