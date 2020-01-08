FROM microsoft/dotnet:3.1-sdk AS build-env
WORKDIR /app
COPY . /app
RUN dotnet publish -c custom_exporter.csproj -o out
FROM microsoft/dotnet:3.1-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/out ./
EXPOSE 8888
ENTRYPOINT ["dotnet", "custom_exporter.dll"]