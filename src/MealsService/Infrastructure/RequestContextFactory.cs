using System;
using System.Threading.Tasks;
using MealsService.Configurations;
using MealsService.Users;
using MealsService.Users.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;

namespace MealsService.Infrastructure
{
    public class RequestContextFactory
    {
        private UsersService _usersService;
        private IOptions<CredentialsConfiguration> _credsConfig;
        private IServiceProvider _serviceProvider;

        public RequestContextFactory(UsersService usersService, IServiceProvider serviceProvider, IOptions<CredentialsConfiguration> credsConfig)
        {
            _usersService = usersService;
            _serviceProvider = serviceProvider;
            _credsConfig = credsConfig;
        }

        public async Task<UserDto> StartRequestContext(int userId)
        {
            var user = await _usersService.GetUserAsync(userId);

            if (user != null)
            {
                var context = _serviceProvider.GetService<RequestContext>();

                context.UserId = userId;
                context.IsAuthenticated = true;
                context.Dtz = user.Timezone != null
                    ? DateTimeZoneProviders.Tzdb.GetZoneOrNull(user.Timezone)
                    : DateTimeZone.Utc;
                context.Timezone = user.Timezone ?? context.Dtz.Id;
                context.Token = _credsConfig.Value.Token;
            }

            return user;
        }

        public void ClearContext()
        {
            var context = _serviceProvider.GetService<RequestContext>();

            context.UserId = 0;
            context.IsAuthenticated = false;
            context.Timezone = "";
            context.Dtz = DateTimeZone.Utc;
            context.Token = null;
        }
    }
}
