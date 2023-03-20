using System.Security.Claims;
using SimpleZ.SignalRManager.Abstractions;

namespace SimpleZ.SignalRManager.LocalConnections.Builder;

public sealed class HubBuilder<TId> : IHubBuilder<TId> where TId : notnull
{
    private readonly HubController<TId> _hubController;

    public HubBuilder()
    {
        _hubController = new HubController<TId>();
        DefineClaimType(ClaimTypes.SerialNumber)
            .AllowedMultiGroupConnection(true)
            .AllowedMultiHubConnection(true);
    }

    public IHubBuilder<TId> DefineClaimType(string claim)
    {
        _hubController.IdClaimType = claim;
        return this;
    }

    public IHubBuilder<TId> AllowedMultiHubConnection(bool allowed)
    {
        _hubController.MultiHubConnection = allowed;
        return this;
    }

    public IHubBuilder<TId> AllowedMultiGroupConnection(bool allowed)
    {
        _hubController.MultiGroupConnection = allowed;
        return this;
    }

    public IHubController<TId> Build() => _hubController;
}