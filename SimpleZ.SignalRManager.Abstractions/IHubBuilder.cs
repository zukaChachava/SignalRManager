namespace SimpleZ.SignalRManager.Abstractions;

public interface IHubBuilder<T> where T : notnull
{
    IHubBuilder<T> DefineClaimType(string claim);
    IHubBuilder<T> AllowedMultiHubConnection(bool allowed);
    IHubBuilder<T> AllowedMultiGroupConnection(bool allowed);
    IHubController<T> Build();
}