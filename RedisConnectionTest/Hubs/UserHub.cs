using Microsoft.AspNetCore.SignalR;
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

    public async Task SendMessageToUser(int userId, string message)
    {
        var connectedUser = await HubController.GetConnectedUserAsync(userId);

        if (connectedUser.ConnectionIds.Any())
            await Task.WhenAll(connectedUser
                .ConnectionIds
                .Select(connections => Clients.User(connections.Key).SendAsync(message)));
    }

    public async Task SendMessageToGroup(string groupName, string message)
    {
        var groupConnections = await HubController.GetGroupConnectionsAsync(groupName);

        if (groupName.Any())
            await Task.WhenAll(groupConnections.Select(connection => Clients.Group(groupName).SendAsync(message)));
    }
}