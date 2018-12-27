using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using MealsService.Configurations;
using MealsService.Infrastructure;
using MealsService.Users.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Users
{
    public class UsersService
    {
        private IOptions<ServicesConfiguration> _servicesConfig;
        private IOptions<CredentialsConfiguration> _credsConfig;
        private IServiceProvider _serviceProvider;

        public UsersService(IOptions<ServicesConfiguration> servicesConfig, IOptions<CredentialsConfiguration> credsConfig, IServiceProvider serviceProvider)
        {
            _servicesConfig = servicesConfig;
            _credsConfig = credsConfig;
            _serviceProvider = serviceProvider;
        }

        public async Task<UserDto> GetUserAsync(int userId)
        {
            UTF8Encoding enc = new UTF8Encoding();

            //Create request
            var request = HttpWebRequest.Create(_servicesConfig.Value.Auth + $"profile/{userId}");
            request.Method = "GET";
            request.ContentType = "application/json";
            request.PreAuthenticate = true;
            request.Headers.Add("Authorization", "Bearer " + _credsConfig.Value.Token);
            
            //Get the response
            try
            {
                var wr = (await request.GetResponseAsync()) as HttpWebResponse;

                if (wr.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = wr.GetResponseStream();
                    StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                    UserDto content = JsonConvert.DeserializeObject<UserDto>(reader.ReadToEnd());

                    return content;
                }
            }
            catch (Exception e) { ; }

            return null;
        }

        public async Task<bool> UpdateJourneyProgressAsync(int userId, int stepId, bool completed)
        {
            var context = _serviceProvider.GetService<RequestContext>();
            
            //Create request
            var request = (HttpWebRequest)WebRequest.Create(_servicesConfig.Value.Auth + $"profile/{userId}/journeyProgress");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.PreAuthenticate = true;
            request.Headers.Add("Authorization", "Bearer " + context.Token);

            //Get the response
            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = "{\"JourneyStepId\":" + stepId +", \"Completed\": "+ completed + "}";

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var wr = (await request.GetResponseAsync()) as HttpWebResponse;

                if (wr.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception e) {; }

            return false;
        }

        public async Task<UserPreferences> GetUserPreferences(int userId)
        {
            //Create request
            var request = HttpWebRequest.Create(_servicesConfig.Value.Auth + $"preferences/{userId}");
            request.Method = "GET";
            request.ContentType = "application/json";
            request.PreAuthenticate = true;
            request.Headers.Add("Authorization", "Bearer " + _credsConfig.Value.Token);
            
            //Get the response
            try
            {
                var wr = (await request.GetResponseAsync()) as HttpWebResponse;

                if (wr.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = wr.GetResponseStream();
                    StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                    UserPreferences content = JsonConvert.DeserializeObject<UserPreferences>(reader.ReadToEnd());

                    return content;
                }
            }
            catch (Exception e) { ; }

            return null;
        }
    }
}

