FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY ["Core/Core.csproj", "Core/"]
COPY ["PostgreSqlMigrationsApplier/PostgreSqlMigrationsApplier.csproj", "PostgreSqlMigrationsApplier/"]
RUN dotnet restore "PostgreSqlMigrationsApplier/PostgreSqlMigrationsApplier.csproj"
COPY . .

FROM build AS publish
WORKDIR "/src/PostgreSqlMigrationsApplier"
RUN dotnet publish "PostgreSqlMigrationsApplier.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PostgreSqlMigrationsApplier.dll"]
