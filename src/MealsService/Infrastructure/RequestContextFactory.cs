
using System;
using System.Threading.Tasks;
using MealsService.Users;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.Infrastructure
{
    public class RequestContextFactory
    {
        private UsersService _usersService;
        private IServiceProvider _serviceProvider;

        public RequestContextFactory(UsersService usersService, IServiceProvider serviceProvider)
        {
            _usersService = usersService;
            _serviceProvider = serviceProvider;
        }

        public async Task StartRequestContext(int userId)
        {
            var user = await _usersService.GetUserAsync(userId);

            var context = _serviceProvider.GetService<RequestContext>();

            context.UserId = userId;
            context.IsAuthenticated = true;
            context.Dtz = user.Timezone != null ? DateTimeZoneProviders.Tzdb.GetZoneOrNull(user.Timezone) : DateTimeZone.Utc;
            context.Timezone = user.Timezone ?? context.Dtz.Id;
        }

        public void ClearContext()
        {
            var context = _serviceProvider.GetService<RequestContext>();

            context.UserId = 0;
            context.IsAuthenticated = false;
            context.Timezone = "";
            context.Dtz = DateTimeZone.Utc;
        }
    }
}
