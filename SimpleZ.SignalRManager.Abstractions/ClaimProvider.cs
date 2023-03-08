using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SimpleZ.SignalRManager.Abstractions;

public class ClaimProvider : IUserIdProvider
{
    private readonly string _claimType;
    
    public ClaimProvider(string claimType)
    {
        _claimType = claimType;
    }
    
    public string GetUserId(HubConnectionContext connection) =>
        connection.User.FindFirst(_claimType)!.Value;
}