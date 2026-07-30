FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RevenueMetrics.API/RevenueMetrics.API.csproj", "RevenueMetrics.API/"]
COPY ["RevenueMetrics.Application/RevenueMetrics.Application.csproj", "RevenueMetrics.Application/"]
COPY ["RevenueMetrics.Domain/RevenueMetrics.Domain.csproj", "RevenueMetrics.Domain/"]
COPY ["RevenueMetrics.Infrastructure/RevenueMetrics.Infrastructure.csproj", "RevenueMetrics.Infrastructure/"]
RUN dotnet restore "RevenueMetrics.API/RevenueMetrics.API.csproj"
COPY . .
WORKDIR "/src/RevenueMetrics.API"
RUN dotnet build "RevenueMetrics.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RevenueMetrics.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RevenueMetrics.API.dll"]
