# SignalR Manager

SignalR Manager is a easy to use library to track connections of **Hubs**.

---

There are two approaches to track connections.

1. Local cache
2. Redis cache


### Local Cache

Installation:
```bash
dotnet add package SimpleZ.SignalRManager.LocalConnections
```

To use local cache only **AddHubController** Extension should be called.

```csharp
builder.Services.AddHubController<int>(config =>
{
    config
        .AllowedMultiGroupConnection(true)
        .AllowedMultiHubConnection(true)
        .DefineClaimType(ClaimTypes.SerialNumber);
});

builder.Services.AddSignalR();
```

Where the generic **int** means that the Unique Id of user is integer type.
The claim type configuration tells the library where the unique id can be found in the users' claims.

Library gives ability to restrict multiple group or hub connections from single user, so that single user can not connect to a
single hub or group with several devices at the same time.

After adding **HubController** Hub object should be declared. For this **MapperHub** must be used which
already connected to the HubController Object.

```csharp
public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
        
    }
}
```

Again Generic **int** refers to the type of user's unique Id;

Inside the MapperHub class ***HubController*** property is available from where several useful
functions are available.

```csharp
public class UserHub : MapperHub<int>
{
    public UserHub(IHubController<int> hubController) : base(hubController)
    {
        
    }
    
    public async Task SendMessageToUser(string userId, string message)
    {
        // Getting connected user's client Ids and groups by user Id
        // The Id is taken from ClaimTypes configured in Program.cs
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
        // Getting client Ids which are connected to this specific Group
        var groupConnections = await HubController.GetGroupConnectionsAsync(groupName);

        if (groupConnections.Any())
            await Clients.Group(groupName).SendAsync("SendMessageToGroup", $"Group -> {groupName}: {message}");
    }
}
```


### Redis Cache

Another way to track Hub connections is redis. 

Installation:
```bash
dotnet add package SimpleZ.SignalRManager.RedisConnections
```

For this following configuration should be used:

```csharp
builder.Services.AddHubController<int>(
    "localhost:6379",
    options =>
    {
        options.AllowedMultiGroupConnection(true)
            .AllowedMultiHubConnection(true)
            .DefineClaimType(ClaimTypes.SerialNumber);
    });
```

The first parameter is the connection string of redis. The Hub configuration is the same mentioned
above.

Also if you want to use SignalR with redis you can use this configuration made by Microsoft:

```csharp
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis("localhost:6379");
```

but this is optional and depends on your needs.

Mapper class is the same here. It still contains ***HubController*** property which has the same
functionality mentioned above.