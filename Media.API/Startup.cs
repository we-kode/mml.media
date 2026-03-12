using Asp.Versioning;
using Autofac;
using Media.API.Filters;
using Media.API.HostedServices;
using Media.API.Middleware;
using Media.API.Services;
using Media.Application.Constants;
using Media.Application.Consumers;
using Media.DBContext;
using Media.Infrastructure.IO;
using Media.Infrastructure.Repositories;
using Media.Infrastructure.Services;
using Messages.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation.SystemNetHttp;
using Rebus.Config;
using System;
using System.IO;
using System.Net.Http;

namespace Media.API;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
public class Startup(IConfiguration configuration)
{
  public IConfiguration Configuration { get; } = configuration;

  private readonly Uri issuer = new(configuration["OpenId:Issuer"] ?? throw new ArgumentNullException("OpenId:Issuer"));

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    Console.WriteLine($"Starting up instance '{Env.INSTANCE}'");

    services.AddMemoryCache();
    ConfigureFolders();
    services.AddControllers();
    ConfigureLocaleServices(services);
    ConfigureApiServices(services);
    ConfigureMBusServices(services);
    ConfigureCorsServices(services);
    ConfigureAuth(services);
    services.AddHostedService<MigrateBitrates>();
    services.AddHostedService<MigrateCovers>();
  }

  private static void ConfigureFolders()
  {
    var folders = new[] { "covers" };
    foreach (var folder in folders)
    {
      var path = Path.Combine("/records", folder);
      if (!Directory.Exists(path))
      {
        Directory.CreateDirectory(path);
      }
    }
  }

  private static void ConfigureLocaleServices(IServiceCollection services)
  {
    services.AddMvc().AddDataAnnotationsLocalization(options =>
    {
      options.DataAnnotationLocalizerProvider = (type, factory) =>
          factory.Create(typeof(Resources.ValidationMessages));
    });
  }

  private static void ConfigureApiServices(IServiceCollection services)
  {
    services.AddApiVersioning(config =>
    {
      config.DefaultApiVersion = new ApiVersion(2.0);
      config.AssumeDefaultVersionWhenUnspecified = true;
    });
    services.AddEndpointsApiExplorer();
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
      // configuring Swagger/OpenAPI. More at https://aka.ms/aspnetcore/swashbuckle
      services.AddSwaggerGen(config =>
     {
       config.SwaggerDoc("v2.0", new OpenApiInfo { Title = "Media Api", Version = "v2.0" });
       config.OperationFilter<RemoveVersionParameterFilter>();
       config.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
       config.EnableAnnotations();
     });
    }
  }

  private void ConfigureMBusServices(IServiceCollection services)
  {
    // Configure Rebus with RabbitMQ transport
    var mBusHost = Configuration["MessageBus:Host"] ?? throw new ArgumentNullException("MessageBus:Host");
    var mBusVirtualHost = Configuration["MessageBus:VirtualHost"];
    var mBusUser = Configuration["MessageBus:User"] ?? throw new ArgumentNullException("MessageBus:User");
    var mBusPassword = Configuration["MessageBus:Password"] ?? throw new ArgumentNullException("MessageBus:Password");

    var mBusConnection = $"amqp://{mBusUser}:{mBusPassword}@{mBusHost}";
    if (!string.IsNullOrEmpty(mBusVirtualHost))
    {
      mBusConnection += $"/{mBusVirtualHost}";
    }

    services.AddRebus(mt =>
      mt.Transport(t => t.UseRabbitMq(mBusConnection, "mml.media.queue")),
      onCreated: async bus =>
      {
        // Hier werden alle Abonnements beim Start einmalig registriert
        await bus.Subscribe<GroupCreated>();
        await bus.Subscribe<GroupDeleted>();
        await bus.Subscribe<GroupUpdated>();
        await bus.Subscribe<ClientStateUpdated>();
      }
    );

    services.AutoRegisterHandlersFromAssemblyOf<GroupConsumer>();
    services.AutoRegisterHandlersFromAssemblyOf<AuthorizationClient>();
  }

  private static void ConfigureCorsServices(IServiceCollection services)
  {
    services.AddCors(options =>
    {
      options.AddDefaultPolicy(builder =>
      {
        builder.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader();
      });
    });
  }

  private void ConfigureAuth(IServiceCollection services)
  {
    services.AddHttpClient<IAuthorizationClient, AuthorizationClient>(c =>
    {
      c.BaseAddress = issuer!;
    })
    .ConfigureHttpClient(c =>
    {
      c.DefaultRequestHeaders.Add("ClientId", Configuration["ApiClient:ClientId"]);
      c.DefaultRequestHeaders.Add("ClientSecret", Configuration["ApiClient:ClientSecret"]);
    });

    services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

    var httpClient = services
      .AddHttpClient(typeof(OpenIddictValidationSystemNetHttpOptions).Assembly.GetName().Name!)
      .ConfigureHttpClient(c =>
      {
        c.DefaultRequestHeaders.Add("ClientId", Configuration["ApiClient:ClientId"]);
        c.DefaultRequestHeaders.Add("ClientSecret", Configuration["ApiClient:ClientSecret"]);
      });

    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
      httpClient
      .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      });
    }

    services.AddAuthorizationBuilder()
      // TODO: Für später, default login policy schließt sync scope aus, damit die sync clients nur auf endpoint für sync nd nicht auf die apis zugreifen.
      //.SetDefaultPolicy(new AuthorizationPolicyBuilder()
      //  .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
      //  .RequireAuthenticatedUser()
      //  .RequireAssertion(ctx =>
      //  {
      //    var scope = ctx.User.FindFirst(OpenIddictConstants.Claims.Scope)?.Value;
      //    return scope == null || !scope.Contains("sync");
      //  })
      // .Build())
      .AddPolicy(Roles.Admin, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Roles.Admin);
      })
      .AddPolicy(Roles.Client, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Roles.Client);
      });
    // TODO: Policy für sync services
    //.AddPolicy("SyncService", policy =>
    //{
    //  policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    //  policy.RequireAuthenticatedUser();
    //  policy.RequireClaim(OpenIddictConstants.Claims.Scope, "sync");
    //}); 

    services.AddOpenIddict()
    .AddValidation(options =>
    {
      options.SetIssuer(issuer);
      options.AddAudiences($"wekode.mml.media/{Env.INSTANCE.ToLower()}");
      options.UseSystemNetHttp();
      options.UseAspNetCore();
    });
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    app.UseApiKeyValidation();
    // Configure the HTTP request pipeline.
    if (env.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI(config =>
      {
        config.SwaggerEndpoint("/swagger/v2.0/swagger.json", "Media API v2.0");
      });
      app.UseDeveloperExceptionPage();
    }
    var supportedCultures = new[] { "en", "en_US", "de", "de_DE", "ru", "ru_RU" };
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    app.UseRequestLocalization(localizationOptions);
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapControllers();
    });
  }

  /// <summary>
  /// Configures the autofac dependency injections for this project.
  /// </summary>
  /// <param name="cBuilder"><see cref="ContainerBuilder"/></param>
  public void ConfigureContainer(ContainerBuilder cBuilder)
  {
    // db context
    ApplicationDBContext factory()
    {
      var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
      optionsBuilder.UseNpgsql(Configuration.GetConnectionString("MediaConnection"));

      return new ApplicationDBContext(optionsBuilder.Options);
    }

    cBuilder.RegisterInstance(factory);
    MigrateDB(factory);

    cBuilder.RegisterType<SqlAlbumRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlArtistRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlGenreRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlLanguageRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlSettingsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlRecordsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlLivestreamRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<RecordService>().AsImplementedInterfaces();
    cBuilder.RegisterType<IndexService>().AsImplementedInterfaces();
    cBuilder.RegisterType<CoverLoader>().AsImplementedInterfaces();
  }

  private static void MigrateDB(Func<ApplicationDBContext> factory)
  {
    using var context = factory();
    if (context.Database.IsRelational())
    {
      context.Database.Migrate();
    }
  }
}
