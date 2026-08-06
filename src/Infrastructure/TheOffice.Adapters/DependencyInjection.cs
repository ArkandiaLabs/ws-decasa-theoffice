using Microsoft.Extensions.DependencyInjection;

using TheOffice.Application.Interfaces.Adapters;
using TheOffice.Adapters.Notification;

namespace TheOffice.Adapters;

public static class DependencyInjection
{
  public static IServiceCollection AddAdapters(this IServiceCollection services)
  {
    services.AddScoped<INotificationAdapter, ConsoleNotificationAdapter>();

    return services;
  }
}
