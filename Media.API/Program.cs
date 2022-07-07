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
                var cert = config["TLS:Cert"];
                var pwd = config["TLS:Password"];
                listenOptions.UseHttps(cert, pwd);
              });
            });
          });
    }
  }
}
