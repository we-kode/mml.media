using Autofac;
using AutoMapper;
using Media.DBContext;
using Media.Filters;
using Media.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

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
    _ConfigureApiServices(services);
    _ConfigureCorsServices(services);
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

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors();

    // TODO
    //app.UseAuthentication();
    //app.UseAuthorization();

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

    // configure your di here
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
