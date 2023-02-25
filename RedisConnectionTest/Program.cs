using System.Security.Claims;
using LocalConnectionTest.Authentication;
using RedisConnectionTest.Authentication;
using RedisConnectionTest.Hubs;
using SimpleZ.SignalRManager.RedisConnections.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<CustomAuthenticationSchemeOptions, CustomAuthenticationHandler>("Basic", null);

builder.Services
    .AddSignalR()
    .AddStackExchangeRedis("localhost:6379");

builder.Services.AddCors(opt => opt.AddDefaultPolicy(options =>
{
    options.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddHubController<int>(
    "localhost:6379",
    options =>
    {
        options.AllowedMultiGroupConnection(true)
            .AllowedMultiHubConnection(true)
            .DefineClaimType(ClaimTypes.SerialNumber);
    });



var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapHub<UserHub>("/hub/users");
app.MapHub<TestHub>("/hub/test");

app.Run();