using Microsoft.AspNetCore.SignalR;
using RedisConnectionTest.Models;
using SimpleZ.SignalRManager.Abstractions;

namespace RedisConnectionTest.Hubs;

public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
    }

    public Task JoinGroup(string groupName) =>
        AddToGroupAsync(groupName);

    public Task LeaveGroup(string groupName) =>
        RemoveFromGroupAsync(groupName);

    public async ValueTask SendMessageToUser(string userId, string message)
    {
        var connectedUser = await HubController.GetConnectedUserAsync(Int32.Parse(userId));
        
        // Message will be sent to all devices connected to this user

        if (connectedUser.ConnectionIds.Any())
            await Clients.User(userId).SendAsync("SendMessageToUser", userId, $"Specific user: {message}");

        // Or can be get specific connection and send a message directly to one device
        
        if (connectedUser.ConnectionIds.Any())
            await Clients.Client(connectedUser.ConnectionIds.First().Key)
                .SendAsync("SendMessageToUser", userId, $"Specific device: {message}");
    }

    public async ValueTask SendMessageToGroup(string groupName, string message)
    {
        var groupConnections = await HubController.GetGroupConnectionsAsync(groupName);

        if (groupConnections.Any())
            await Clients.Group(groupName).SendAsync("SendMessageToGroup", $"Group -> {groupName}: {message}");
    }
}