using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var keyVaultConfigBuilder = new ConfigurationBuilder();

                    keyVaultConfigBuilder.AddAzureKeyVault(
                        $"https://{Environment.GetEnvironmentVariable("KEYVAULT_NAME")}.vault.azure.net/",
                        Environment.GetEnvironmentVariable("KEYVAULT_CLIENT_ID"),
                        Environment.GetEnvironmentVariable("KEYVAULT_CLIENT_SECRET"));

                    builder.AddConfiguration(keyVaultConfigBuilder.Build());
                })
                .UseStartup<Startup>();
    }
}