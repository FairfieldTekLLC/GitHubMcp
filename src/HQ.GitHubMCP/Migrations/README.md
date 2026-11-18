Generate the initial production-reviewed migration with:

dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef migrations add InitialCreate --project src/HQ.GitHubMCP
dotnet ef migrations script --idempotent --project src/HQ.GitHubMCP -o HQ_GitHubMCP.sql

Review the SQL script and deploy it through your normal SQL Server change process. Do not use automatic migrations in production.