using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using MealsService.Configurations;
using MealsService.Infrastructure;
using MealsService.Subscriptions.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.Users
{
    public class SubscriptionsService
    {
        private IOptions<ServicesConfiguration> _servicesConfig;
        private IServiceProvider _serviceProvider;

        private Dictionary<int, UserSubscription> _cache = new Dictionary<int, UserSubscription>();

        public SubscriptionsService(IOptions<ServicesConfiguration> servicesConfig, IServiceProvider serviceProvider)
        {
            _servicesConfig = servicesConfig;
            _serviceProvider = serviceProvider;
        }

        public async Task<UserSubscription> GetUserSubscription(int userId, bool skipCache = false)
        {
            if (!skipCache && _cache.ContainsKey(userId))
            {
                return _cache[userId];
            }

            var context = _serviceProvider.GetService<RequestContext>();

            //Create request
            var request = HttpWebRequest.Create(_servicesConfig.Value.Auth + $"subscriptions/{userId}");
            request.Method = "GET";
            request.ContentType = "application/json";
            request.PreAuthenticate = true;
            request.Headers.Add("Authorization", "Bearer " + context.Token);
            
            //Get the response
            try
            {
                var wr = (await request.GetResponseAsync()) as HttpWebResponse;

                if (wr != null && wr.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = wr.GetResponseStream();
                    StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                    UserSubscription content = JsonConvert.DeserializeObject<UserSubscription>(reader.ReadToEnd());

                    _cache.Add(userId, content);

                    return content;
                }
            }
            catch (Exception e) { ; }

            return null;
        }

        public async Task VerifyDateInSubscriptionAsync(int userId, LocalDate date)
        {
            var reqContext = _serviceProvider.GetService<RequestContext>();
            var curInstant = SystemClock.Instance.GetCurrentInstant();
            var dateInstant = date.GetWeekStart().Minus(Period.FromDays(1))
                .AtStartOfDayInZone(reqContext.Dtz).ToInstant();

            var sub = await GetUserSubscription(userId);

            if (sub.UserId != userId ||
                (sub.Status != SubscriptionStatus.ACTIVE &&
                sub.Status != SubscriptionStatus.TRIAL &&
                dateInstant > curInstant))
            {
                throw SubscriptionErrors.MissingSubscription;
            }
        }
    }
}

