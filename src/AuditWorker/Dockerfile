# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/AuditWorker/AuditWorker.csproj
RUN dotnet publish src/AuditWorker/AuditWorker.csproj -c Release -o /app/publish

# Run
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditWorker.dll"]
