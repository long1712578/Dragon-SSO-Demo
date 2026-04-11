FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["DragonSSO.sln", "."]
COPY ["src/IdentityService.API/IdentityService.API.csproj", "src/IdentityService.API/"]
COPY ["src/IdentityService.Application/IdentityService.Application.csproj", "src/IdentityService.Application/"]
COPY ["src/IdentityService.Application.Contracts/IdentityService.Application.Contracts.csproj", "src/IdentityService.Application.Contracts/"]
COPY ["src/IdentityService.Domain/IdentityService.Domain.csproj", "src/IdentityService.Domain/"]
COPY ["src/IdentityService.Domain.Shared/IdentityService.Domain.Shared.csproj", "src/IdentityService.Domain.Shared/"]
COPY ["src/IdentityService.EntityFrameworkCore/IdentityService.EntityFrameworkCore.csproj", "src/IdentityService.EntityFrameworkCore/"]
COPY ["src/IdentityService.HttpApi/IdentityService.HttpApi.csproj", "src/IdentityService.HttpApi/"]

RUN dotnet restore "src/IdentityService.API/IdentityService.API.csproj"

COPY . .
RUN dotnet publish "src/IdentityService.API/IdentityService.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "IdentityService.API.dll"]
