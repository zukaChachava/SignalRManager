using Microsoft.AspNetCore.SignalR;

namespace RedisConnectionTest.Hubs;

public class TestHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine("Connection");
        return base.OnConnectedAsync();
    }
}