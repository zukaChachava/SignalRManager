using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LocalConnectionTest.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace RedisConnectionTest.Authentication;

public class CustomAuthenticationHandler : AuthenticationHandler<CustomAuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<CustomAuthenticationSchemeOptions> _options;

    public CustomAuthenticationHandler(
        IOptionsMonitor<CustomAuthenticationSchemeOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, 
        ISystemClock clock
        ) 
        : base(options, logger, encoder, clock)
    {
        _options = options;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out AuthenticationHeaderValue headerValue))
        {
            try
            {
                var ticket = GenerateTicket(headerValue.Parameter!);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail("Not valid"));
            }
        }
        
        if (!StringValues.IsNullOrEmpty(Request.Query["access_token"]))
        {
            try
            {
                var token = Request.Query["access_token"];
                var ticket = GenerateTicket(token);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail("Not valid"));
            }
        }
        
        return Task.FromResult(AuthenticateResult.Fail("Not valid"));
    }
    
    private AuthenticationTicket GenerateTicket(string token)
    {
        var id = Convert.ToInt32(token);
        var claims = new Claim[] { new Claim(ClaimTypes.SerialNumber, id.ToString()) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }
}