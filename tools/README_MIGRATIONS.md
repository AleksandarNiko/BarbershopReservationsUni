This project includes new model OneTimeCode and updates to the DbContext.

To create and apply the EF Core migration locally, run from the Web project directory:

1. dotnet tool restore
2. dotnet ef migrations add AddOneTimeCodesAndBookingFixes -o Data/Migrations
3. dotnet ef database update

If your solution uses a separate startup project, pass -s and -p accordingly.

If you want, I can generate a migration file for you here, but applying it to your local database requires running the commands above.
