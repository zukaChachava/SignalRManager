using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.SignalR;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections;

namespace LocalConnectionTest.Hubs;

public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
        
    }
    
    public async Task SendMessageToUser(string userId, string message)
    {
        var connectedUser = await HubController.GetConnectedUserAsync(Int32.Parse(userId));
        
        if(connectedUser == null)
            return;
        
        // Message will be sent to all devices connected to this user

        if (connectedUser.ConnectionIds.Any())
            await Clients.User(userId).SendAsync("SendMessageToUser", userId, $"Specific user: {message}");

        // Or can be get specific connection and send a message directly to one device
        
        if (connectedUser.ConnectionIds.Any())
            await Clients.Client(connectedUser.ConnectionIds.First().Key)
                .SendAsync("SendMessageToUser", userId, $"Specific device: {message}");
    }

    public async Task SendMessageToGroup(string groupName, string message)
    {
        var groupConnections = await HubController.GetGroupConnectionsAsync(groupName);

        if (groupConnections.Any())
            await Clients.Group(groupName).SendAsync("SendMessageToGroup", $"Group -> {groupName}: {message}");
    }
}