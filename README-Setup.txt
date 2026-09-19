# Capstone Rental System

ASP.NET Core MVC / .NET 8 project.

## First run

1. Check the SQL Server connection in `appsettings.json`.
2. Open the project in Visual Studio.
3. Restore NuGet packages.
4. Run the project.

`Program.cs` applies pending Entity Framework Core migrations automatically at startup, including the return-inspection fields.

If your database migration history says the return migration was already applied but the columns are still missing, use `Database_Update_ReturnInspection.sql` manually in SQL Server Management Studio.

## Item photo path

`wwwroot/Images/Items/<CatID>/<ItemCode>.jpg`

Example:
`wwwroot/Images/Items/1/GOW-001.jpg`

The customer dashboard displays the item photo, name, category, size, price, and availability.
