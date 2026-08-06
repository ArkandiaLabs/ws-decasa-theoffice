using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TheOffice.Application.Interfaces.Persistence;
using TheOffice.Persistence.Repositories;

namespace TheOffice.Persistence;

public static class DependencyInjection
{
  public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<TheOfficeDbContext>(
      options => options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

    services.AddScoped<ICustomerRepository, CustomerRepository>();
    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();

    return services;
  }
}
