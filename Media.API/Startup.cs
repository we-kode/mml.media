using Autofac;
using AutoMapper;
using MassTransit;
using Media.API.Contracts;
using Media.Application.Consumers;
using Media.Application.Models;
using Media.DBContext;
using Media.Filters;
using Media.Infrastructure;
using Media.Middleware;
using Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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

namespace Media.API;
public class Startup
{
  public Startup(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public IConfiguration Configuration { get; }

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddControllers();
    _ConfigureLocaleServices(services);
    _ConfigureApiServices(services);
    _ConfigureMBusServices(services);
    _ConfigureCorsServices(services);
    _ConfigureAuth(services);
  }

  private void _ConfigureLocaleServices(IServiceCollection services)
  {
    services.AddMvc().AddDataAnnotationsLocalization(options =>
    {
      options.DataAnnotationLocalizerProvider = (type, factory) =>
          factory.Create(typeof(Resources.ValidationMessages));
    });
  }

  private void _ConfigureApiServices(IServiceCollection services)
  {
    services.AddApiVersioning(config =>
    {
      config.DefaultApiVersion = new ApiVersion(1, 0);
      config.AssumeDefaultVersionWhenUnspecified = true;
    });
    services.AddEndpointsApiExplorer();
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
      // configuring Swagger/OpenAPI. More at https://aka.ms/aspnetcore/swashbuckle
      services.AddSwaggerGen(config =>
     {
       config.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Media Api", Version = "v1.0" });
       config.OperationFilter<RemoveVersionParameterFilter>();
       config.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
       config.EnableAnnotations();
     });
    }
  }

  private void _ConfigureMBusServices(IServiceCollection services)
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
          h.Username(Configuration["MassTransit:User"]);
          h.Password(Configuration["MassTransit:Password"]);
        });

        cfg.ConfigureEndpoints(context);
      });
    });
    services.AddOptions<MassTransitHostOptions>()
      .Configure(options =>
      {
        options.WaitUntilStarted = bool.Parse(Configuration["MassTransit:WaitUntilStarted"]);
        options.StartTimeout = TimeSpan.FromSeconds(double.Parse(Configuration["MassTransit:StartTimeoutSeconds"]));
        options.StopTimeout = TimeSpan.FromSeconds(double.Parse(Configuration["MassTransit:StopTimeoutSeconds"]));
      });
  }

  private void _ConfigureCorsServices(IServiceCollection services)
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

  private void _ConfigureAuth(IServiceCollection services)
  {
    services
      .AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

    var httpClient = services
      .AddHttpClient(typeof(OpenIddictValidationSystemNetHttpOptions).Assembly.GetName().Name)
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

    services.AddAuthorization(option =>
    {
      option.AddPolicy(Application.Constants.Roles.Admin, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Application.Constants.Roles.Admin);
      });
      option.AddPolicy(Application.Constants.Roles.Client, policy =>
      {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(OpenIddictConstants.Claims.Role, Application.Constants.Roles.Client);
      });
    });
    services.AddOpenIddict()
    .AddValidation(options =>
    {
      options.SetIssuer(new Uri(Configuration["OpenId:Issuer"]));
      options.AddAudiences(Configuration["ApiClient:ClientId"]);
      options.AddEncryptionCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(Configuration["OpenId:EncryptionCert"]));
      options.UseIntrospection()
               .SetClientId(Configuration["ApiClient:ClientId"])
               .SetClientSecret(Configuration["ApiClient:ClientSecret"]);
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
    Func<ApplicationDBContext> factory = () =>
    {
      var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
      optionsBuilder.UseNpgsql(Configuration.GetConnectionString("MediaConnection"));

      return new ApplicationDBContext(optionsBuilder.Options);
    };

    cBuilder.RegisterInstance(factory);
    _MigrateDB(factory);

    // automapper
    cBuilder.Register(context => new MapperConfiguration(cfg =>
    {
      // configure automapping classes here
      cfg.CreateMap<GroupCreated, Group>();
      cfg.CreateMap<GroupUpdated, Group>();
      cfg.CreateMap<TagFilter, Application.Contracts.TagFilter>();
      cfg.CreateMap<DBContext.Models.Albums, Album>();
      cfg.CreateMap<DBContext.Models.Genres, Genre>();
      cfg.CreateMap<DBContext.Models.Genres, GenreBitrate>();
      cfg.CreateMap<DBContext.Models.Artists, Artist>();
      cfg.CreateMap<DBContext.Models.Languages, Language>();
      cfg.CreateMap<RecordChangeRequest, Record>();
      cfg.CreateMap<DBContext.Models.Livestreams, Livestream>();
      cfg.CreateMap<DBContext.Models.Livestreams, LivestreamSettings>();
      cfg.CreateMap<LivestreamChangeRequest, LivestreamSettings>();
      cfg.CreateMap<SettingsRequest, Settings>();
      cfg.CreateMap<Contracts.RecordFolder, Application.Models.RecordFolder>();
      cfg.CreateMap<DBContext.Models.Groups, Group>()
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

    cBuilder.RegisterType<SqlSettingsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlRecordsRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();
    cBuilder.RegisterType<SqlLivestreamRepository>().AsImplementedInterfaces();
  }

  private void _MigrateDB(Func<ApplicationDBContext> factory)
  {
    using var context = factory();
    if (context.Database.IsRelational())
    {
      context.Database.Migrate();
    }
  }
}
