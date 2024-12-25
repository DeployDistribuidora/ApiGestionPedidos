using System.IdentityModel.Tokens.Jwt;
using System.Linq;

public static class JwtHelper
{
    public static string GetClaim(string token, string claimType)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        var claim = jwtToken?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        return claim;
    }
}
