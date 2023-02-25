using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SimpleZ.SignalRManager.Abstractions;

[Authorize]
public abstract class MapperHub<TId> : Hub
{
    private readonly IHubController<TId> _hubController;

    protected MapperHub(IHubController<TId> hubController)
    {
        _hubController = hubController;
    }

    protected IHubController<TId> HubController => _hubController;

    protected Task AddToGroupAsync(string group)
    {
        return Task.WhenAll(
            Groups.AddToGroupAsync(Context.ConnectionId, group),
            _hubController.AddUserToGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    protected Task RemoveFromGroupAsync(string group)
    {
        return Task.WhenAll(
            Groups.RemoveFromGroupAsync(Context.ConnectionId, group),
            _hubController.RemoveUserFromGroupAsync(group, GetUserId(), Context.ConnectionId)
        );
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        TId id = GetUserId();
        await _hubController.AddUserAsync(id, Context.ConnectionId, this.GetType());
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        TId id = GetUserId();
        await _hubController.RemoveUserAsync(id, Context.ConnectionId, this.GetType());
        await base.OnDisconnectedAsync(exception);
    }

    protected Task AlertGroup(string group, string funcName, string text) =>
         Clients.Group(group).SendAsync(funcName, text);

    private TId GetUserId()
    {
        return ((TId)Convert.ChangeType(Context.User.FindFirst(_hubController.IdClaimType!)!.Value, typeof(TId)))
               ?? throw new Exception("Can not convert User Claim into User ID");
    }
}