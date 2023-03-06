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

        if (connectedUser.ConnectionIds.Any())
            await Clients.Users(connectedUser.ConnectionIds.Select(connectionData => connectionData.Key).ToArray())
                .SendAsync("SendMessageToUser", userId, message);
    }

    public async ValueTask SendMessageToGroup(string groupName, string message)
    {
        var groupConnections = await HubController.GetGroupConnectionsAsync(groupName);

        if (groupConnections.Any())
            await Clients.Group(groupName).SendAsync("SendMessageToGroup", $"Group -> {groupName}: {message}");
    }
}