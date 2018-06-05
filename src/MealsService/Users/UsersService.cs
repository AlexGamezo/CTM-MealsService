
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MealsService.Configurations;
using MealsService.Users.Data;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MealsService.Users
{
    public class UsersService
    {
        private IOptions<ServicesConfiguration> _servicesConfig;
        private IOptions<CredentialsConfiguration> _credsConfig;

        public UsersService(IOptions<ServicesConfiguration> servicesConfig, IOptions<CredentialsConfiguration> credsConfig)
        {
            _servicesConfig = servicesConfig;
            _credsConfig = credsConfig;
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
            var wr = (await request.GetResponseAsync()) as HttpWebResponse;

            if (wr.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = wr.GetResponseStream();
                StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
                UserDto content = JsonConvert.DeserializeObject<UserDto>(reader.ReadToEnd());

                return content;
            }
            else
            {
                ;
            }

            return null;
        }
    }
}
