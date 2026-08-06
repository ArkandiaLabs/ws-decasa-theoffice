using TheOffice.Application.Interfaces.Adapters;

namespace TheOffice.Adapters.Notification;

public class ConsoleNotificationAdapter : INotificationAdapter
{
  public async Task SendMessage(string message)
  {
    await Task.Run(() => Console.WriteLine(message));
  }
}
