namespace TheOffice.Application.Interfaces.Adapters;

public interface INotificationAdapter
{
  Task SendMessage(string message);
}
