FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VoltElectronics.sln ./
COPY src/VoltElectronics.Domain/VoltElectronics.Domain.csproj src/VoltElectronics.Domain/
COPY src/VoltElectronics.Application/VoltElectronics.Application.csproj src/VoltElectronics.Application/
COPY src/VoltElectronics.Infrastructure/VoltElectronics.Infrastructure.csproj src/VoltElectronics.Infrastructure/
COPY src/VoltElectronics.Api/VoltElectronics.Api.csproj src/VoltElectronics.Api/
COPY tests/VoltElectronics.Tests/VoltElectronics.Tests.csproj tests/VoltElectronics.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/VoltElectronics.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VoltElectronics.Api.dll"]
