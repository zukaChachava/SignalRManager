using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using SimpleZ.SignalRManager.Abstractions;

namespace SimpleZ.SignalRManager.LocalConnections;

[Authorize]
public abstract class MapperHub<TId> : Hub
{
    private readonly IHubController<TId> _hubController;

    protected MapperHub(IHubController<TId> hubController)
    {
        _hubController = hubController;
    }

    protected IHubController<TId> HubController => _hubController;

    public Task AddToGroup(string group)
    {
        return Task.WhenAll(
            Groups.Add(Context.ConnectionId, group),
            _hubController.AddClientToGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    public Task RemoveFromGroup(string group)
    {
        return Task.WhenAll(
            Groups.Remove(Context.ConnectionId, group),
            _hubController.RemoveClientFromGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    public override async Task OnConnected()
    {
        TId id = GetUserId();
        await base.OnConnected();
        await _hubController.AddClientAsync(id, Context.ConnectionId, this.GetType());
    }

    public override async Task OnDisconnected(bool stopCalled)
    {
        TId id = GetUserId();
        await _hubController.RemoveClientAsync(id, Context.ConnectionId, this.GetType());
        await base.OnDisconnected(stopCalled);
    }

    protected Task AlertGroup(string group, string funcName, string text) =>
         Clients.Group(group).SendAsync(funcName, text);

    protected TId GetUserId()
    {
        if (Context.User.Identity is ClaimsIdentity identity)
        {
            var claim = identity.Claims.First(claim => claim.Value == _hubController.GetClientIdClaim);
            return (TId) Convert.ChangeType(claim.Value, typeof(TId));
        }

        throw new Exception("User Id not found in claims");
    }
}