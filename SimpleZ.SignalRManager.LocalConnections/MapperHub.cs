using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
            Groups.AddToGroupAsync(Context.ConnectionId, group),
            _hubController.AddClientToGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    public Task RemoveFromGroup(string group)
    {
        return Task.WhenAll(
            Groups.RemoveFromGroupAsync(Context.ConnectionId, group),
            _hubController.RemoveClientFromGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    public override async Task OnConnectedAsync()
    {
        TId id = GetUserId();
        await base.OnConnectedAsync();
        await _hubController.AddClientAsync(id, Context.ConnectionId, this.GetType());
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        TId id = GetUserId();
        await _hubController.RemoveClientAsync(id, Context.ConnectionId, this.GetType());
        await base.OnDisconnectedAsync(exception);
    }

    protected Task AlertGroup(string group, string funcName, string text) =>
         Clients.Group(group).SendAsync(funcName, text);

    protected TId GetUserId()
    {
        return ((TId)Convert.ChangeType(Context.User.FindFirst(_hubController.GetClientIdClaim)?.Value, typeof(TId)))
               ?? throw new Exception("Can not convert User Claim into User ID");
    }
}