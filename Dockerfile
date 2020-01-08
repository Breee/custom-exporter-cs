FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app
COPY . /app
RUN dotnet publish -c custom_exporter.csproj -o out
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine
WORKDIR /app
COPY --from=build-env /app/out ./
EXPOSE 8888
ENTRYPOINT ["dotnet", "custom_exporter.dll"]
