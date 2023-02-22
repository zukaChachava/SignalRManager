using SimpleZ.SignalRManager.Abstractions;

namespace RedisConnectionTest.Hubs;

public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
        
    }
}