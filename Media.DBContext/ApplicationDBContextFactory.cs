using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace Media.DBContext;

public class ApplicatioDBContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
{
  private const string _DesignTimeConnectionString = "DBServer";

  public ApplicationDBContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
    optionsBuilder.UseNpgsql(_DesignTimeConnectionString, options => options.MigrationsAssembly(typeof(ApplicationDBContext).GetTypeInfo().Assembly.GetName().Name));

    return new ApplicationDBContext(optionsBuilder.Options);
  }
}
