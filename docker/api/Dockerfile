
# Etapa 1: compilação e publicação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore src/Roadmaps.Api/Roadmaps.Api.csproj

RUN dotnet publish src/Roadmaps.Api/Roadmaps.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Etapa 2: execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Roadmaps.Api.dll"]
