using System.Security.Claims;
using LocalConnectionTest.Authentication;
using LocalConnectionTest.Hubs;
using SimpleZ.SignalRManager.LocalConnections.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<CustomAuthenticationSchemeOptions, CustomAuthenticationHandler>("Basic", null);

builder.Services.AddHubController<int>(config =>
{
    config
        .AllowedMultiGroupConnection(true)
        .AllowedMultiHubConnection(true)
        .DefineClaimType(ClaimTypes.SerialNumber);
});

builder.Services.AddSignalR();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(opt =>
{
    opt.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapHub<UserHub>("/hub/users");

app.Run();