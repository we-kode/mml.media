using Autofac;
using AutoMapper;
using MassTransit;
using Media.API.Contracts;
using Media.Application.Consumers;
using Media.Application.Models;
using Media.DBContext;
using Media.Filters;
using Media.Middleware;
using Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Net.Http;
using OpenIddict.Validation.SystemNetHttp;
using Media.API.HostedServices;
using Asp.Versioning;
using Media.Infrastructure.Repositories;
using Media.Application.Contracts.Repositories;

namespace Media.API;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
public class Startup(IConfiguration configuration)
{
  public IConfiguration Configuration { get; } = configuration;

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddControllers();
    ConfigureLocaleServices(services);
    ConfigureApiServices(services);
    ConfigureMBusServices(services);
    ConfigureCorsServices(services);
    ConfigureAuth(services);
    services.AddHostedService<MigrateBitrates>();
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
    services.AddMassTransit(mt =>
    {
      mt.AddConsumer<IndexingRecordConsumer>(cc =>
      {
        if (int.TryParse(Configuration["MassTransit:ConcurrentMessageLimit"], out var limit))
        {
          cc.ConcurrentMessageLimit = limit;
        }
      });
      mt.AddConsumer<GroupConsumer>();
      mt.UsingRabbitMq((context, cfg) =>
      {
        cfg.Host(Configuration["MassTransit:Host"], Configuration["MassTransit:VirtualHost"], h =>
        {
          h.Username(Configuration["MassTransit:User"] ?? throw new ArgumentNullException("MassTransit:User"));
          h.Password(Configuration["MassTransit:Password"] ?? throw new ArgumentNullException("MassTransit:Password"));
        });

        cfg.ConfigureEndpoints(context);
      });
    });
    services.AddOptions<MassTransitHostOptions>()
      .Configure(options =>
      {
        options.WaitUntilStarted = bool.Parse(Configuration["MassTransit:WaitUntilStarted"] ?? "True");
        options.StartTimeout = TimeSpan.FromSeconds(double.Parse(Configuration["MassTransit:StartTimeoutSeconds"] ?? "60"));
        options.StopTimeout = TimeSpan.FromSeconds(double.Parse(Configuration["MassTransit:StopTimeoutSeconds"] ?? "60"));
      });
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
    services
      .AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

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
      .AddPolicy(Application.Constants.Roles.Admin, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Application.Constants.Roles.Admin);
      })
      .AddPolicy(Application.Constants.Roles.Client, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Application.Constants.Roles.Client);
      });
    services.AddOpenIddict()
    .AddValidation(options =>
    {
      options.SetIssuer(new Uri(Configuration["OpenId:Issuer"] ?? throw new ArgumentNullException("OpenId:Issuer")));
      options.AddAudiences(Configuration["ApiClient:ClientId"] ?? throw new ArgumentNullException("ApiClient:ClientId"));
      options.AddEncryptionCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(Configuration["OpenId:EncryptionCert"] ?? throw new ArgumentNullException("OpenId:EncryptionCert")));
      options.UseIntrospection()
               .SetClientId(Configuration["ApiClient:ClientId"] ?? string.Empty)
               .SetClientSecret(Configuration["ApiClient:ClientSecret"] ?? string.Empty);
      options.UseAspNetCore();
      options.UseSystemNetHttp();
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
        config.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Media API v1.0");
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

    // automapper
    cBuilder.Register(context => new MapperConfiguration(cfg =>
    {
      // configure automapping classes here
      cfg.CreateMap<GroupCreated, Group>();
      cfg.CreateMap<GroupUpdated, Group>();
      cfg.CreateMap<Application.Contracts.Repositories.TagFilter, Application.Contracts.Repositories.TagFilter>();
      cfg.CreateMap<DBContext.Models.Album, Album>();
      cfg.CreateMap<DBContext.Models.Genre, Genre>();
      cfg.CreateMap<DBContext.Models.Genre, GenreBitrate>();
      cfg.CreateMap<DBContext.Models.Artist, Artist>();
      cfg.CreateMap<DBContext.Models.Language, Language>();
      cfg.CreateMap<RecordChangeRequest, Record>();
      cfg.CreateMap<DBContext.Models.Livestream, Livestream>();
      cfg.CreateMap<DBContext.Models.Livestream, LivestreamSettings>();
      cfg.CreateMap<LivestreamChangeRequest, LivestreamSettings>();
      cfg.CreateMap<SettingsRequest, Settings>();
      cfg.CreateMap<Contracts.RecordFolder, Application.Models.RecordFolder>();
      cfg.CreateMap<DBContext.Models.Group, Group>()
        .ConstructUsing(g => new Group(g.GroupId, g.Name, g.IsDefault));
    })).AsSelf().SingleInstance();
    cBuilder.Register(c =>
    {
      //This resolves a new context that can be used later.
      var context = c.Resolve<IComponentContext>();
      var config = context.Resolve<MapperConfiguration>();
      return config.CreateMapper(context.Resolve);
    })
    .As<IMapper>()
    .InstancePerLifetimeScope();

    cBuilder.RegisterType<SqlAlbumRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlArtistRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlGenreRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlLanguageRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlSettingsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlRecordsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlLivestreamRepository>().AsImplementedInterfaces();
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
