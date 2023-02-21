using Microsoft.AspNet.SignalR;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections;

namespace LocalConnectionTest.Hubs;

public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
        
    }
}