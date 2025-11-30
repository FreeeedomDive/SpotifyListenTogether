export ASPNETCORE_ENVIRONMENT=Production
read -p "Enter migration name: " name
dotnet tool install --global dotnet-ef
dotnet ef migrations add "$name" --project Core --startup-project PostgreSqlMigrationsApplier