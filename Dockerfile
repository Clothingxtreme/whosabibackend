# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/WhoSabi.Api/WhoSabi.Api.csproj", "src/WhoSabi.Api/"]
COPY ["src/WhoSabi.Application/WhoSabi.Application.csproj", "src/WhoSabi.Application/"]
COPY ["src/WhoSabi.Contracts/WhoSabi.Contracts.csproj", "src/WhoSabi.Contracts/"]
COPY ["src/WhoSabi.Domain/WhoSabi.Domain.csproj", "src/WhoSabi.Domain/"]
COPY ["src/WhoSabi.Infrastructure/WhoSabi.Infrastructure.csproj", "src/WhoSabi.Infrastructure/"]
RUN dotnet restore "src/WhoSabi.Api/WhoSabi.Api.csproj"

COPY . .
RUN dotnet publish "src/WhoSabi.Api/WhoSabi.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "WhoSabi.Api.dll"]
