using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LocalConnectionTest.Authentication;

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
                var id = Convert.ToInt32(headerValue.Parameter);
                var claims = new Claim[] { new Claim(ClaimTypes.SerialNumber, id.ToString()) };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail("Not valid"));
            }
        }
        return Task.FromResult(AuthenticateResult.Fail("Not valid"));
    }
}