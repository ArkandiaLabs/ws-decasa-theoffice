using Microsoft.Extensions.DependencyInjection;

using TheOffice.Adapters.Notification;
using TheOffice.Application.Interfaces.Adapters;

namespace TheOffice.Adapters;

public static class DependencyInjection
{
  public static IServiceCollection AddAdapters(this IServiceCollection services)
  {
    services.AddScoped<INotificationAdapter, ConsoleNotificationAdapter>();

    return services;
  }
}
