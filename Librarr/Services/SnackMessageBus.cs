using MudBlazor;

namespace Librarr.Services;

public class SnackMessageBus
{
    public event Action<string, Severity>? MessageReceived;

    public void ShowInfo(string message)
    {
        MessageReceived?.Invoke(message, Severity.Info);
    }
}