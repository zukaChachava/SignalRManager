using LocalConnectionTest.Authentication;
using LocalConnectionTest.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<CustomAuthenticationSchemeOptions, CustomAuthenticationHandler>("Basic", null);

builder.Services.AddSignalR();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapHub<UserHub>("/hub/users");

app.Run();