using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace MealsService.Infrastructure
{
    public class RequestContext
    {
        public static string DEFAULT_TIMEZONE = "America/Los_Angeles";

        public bool IsAuthenticated { get; set; }

        public int UserId { get; set; }
        public string Timezone { get; set; } = DEFAULT_TIMEZONE;

        public DateTimeZone Dtz { get; private set; }

        public void Hydrate(HttpContext httpContext)
        {
            var claims = httpContext.User.Claims.ToList();

            if (claims.Any())
            {
                Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value,
                    out var userId);

                IsAuthenticated = true;
                UserId = userId;
                Timezone = claims.FirstOrDefault(c => c.Type == "tz")?.Value ?? DEFAULT_TIMEZONE;
            }

            Dtz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(Timezone);
        }
    }
}
