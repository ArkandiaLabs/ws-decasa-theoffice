using Microsoft.Extensions.DependencyInjection;

using TheOffice.Application.Services;

namespace TheOffice.Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddScoped<CustomerService>();
    services.AddScoped<CategoryService>();
    services.AddScoped<ProductService>();

    return services;
  }
}
