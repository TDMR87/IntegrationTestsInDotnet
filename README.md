## ENTITY FRAMEWORK CORE

Run EF Core migrations inside the ```root``` directory, for example:

```bash
dotnet ef migrations add InitialMigration --project ./src/Bloqqer.Database/Bloqqer.Database.csproj --startup-project ./src/Bloqqer.Api/Threadis.Api.csproj

dotnet ef database update --project ./src/Backend/Threadis.Database/Threadis.Database.csproj --startup-project ./src/Backend/Threadis.Web/Threadis.Web.csproj
```