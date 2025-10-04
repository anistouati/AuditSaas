# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/AuditService/AuditService.csproj
RUN dotnet publish src/AuditService/AuditService.csproj -c Release -o /app/publish

# Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final   
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "AuditService.dll"]
