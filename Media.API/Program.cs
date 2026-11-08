using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Media.API
{
  public class Program
  {

    public static bool IsTest()
    {
      return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test";
    }

    /// <summary>
    /// Determines whether the service listens on https, which is the case when a certificate is configured in <c>TLS:Cert</c>.
    /// </summary>
    public static bool UseTls(IConfiguration configuration)
    {
      return !string.IsNullOrWhiteSpace(configuration["TLS:Cert"]);
    }

    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddJsonFile(IsTest() ? "./test.appsettings.json" : "/configs/appsettings.json")
        .Build();

      return Host.CreateDefaultBuilder(args)
          .UseServiceProviderFactory(new AutofacServiceProviderFactory())
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseConfiguration(config);
            webBuilder.UseStartup<Startup>();
            webBuilder.ConfigureKestrel(options =>
            {
              options.ListenAnyIP(5052, listenOptions =>
              {
                if (UseTls(config))
                {
                  listenOptions.UseHttps(config["TLS:Cert"]!, config["TLS:Password"]);
                }
                else
                {
                  Console.WriteLine("No 'TLS:Cert' configured. Listening on http, TLS has to be terminated by the reverse proxy.");
                }
              });
            });
          });
    }
  }
}
