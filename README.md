## ENTITY FRAMEWORK CORE

Run EF Core migrations inside the ```root``` directory, for example:

```bash
dotnet ef migrations add InitialMigration --project ./src/Bloqqer.Database/Bloqqer.Database.csproj --startup-project ./src/Bloqqer.Api/Bloqqer.Api.csproj

dotnet ef database update --project ./src/Bloqqer.Database/Bloqqer.Database.csproj  --startup-project ./src/Bloqqer.Api/Bloqqer.Api.csproj
```