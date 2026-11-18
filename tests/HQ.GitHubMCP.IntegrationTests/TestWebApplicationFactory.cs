using HQ.GitHubMCP.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HQ.GitHubMCP.IntegrationTests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientCertificates:0:SourceType"] = "Path",
                ["AzureAd:ClientCertificates:0:CertificatePath"] = "HQ.GitHubMCP-EntraAuth.pfx",
                ["AzureAd:ClientCertificates:0:CertificatePassword"] = "test-password",
                ["GitHub:AuthenticationMode"] = "GitHubApp",
                ["GitHub:AppId"] = "123456",
                ["GitHub:InstallationId"] = "12345678",
                ["GitHub:PrivateKeyPath"] = "hq-githubmcp.2026-11-17.private-key.pem",
                ["GitHub:ApiBaseUrl"] = "https://api.github.com/"
            };

            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            services.AddSingleton(connection);

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
