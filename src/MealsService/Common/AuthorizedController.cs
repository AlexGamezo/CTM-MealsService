using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.Common
{
    public class AuthorizedController : Controller
    {
        public int AuthorizedUser
        {
            get
            {
                var claims = HttpContext.User.Claims;
                Int32.TryParse(claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value, out var userId);

                return userId;
            }
        }

        public bool IsAdmin
        {
            get
            {
                var claims = HttpContext.User.Claims;
                Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out var isAdmin);

                return isAdmin;
            }
        }
    }
}
