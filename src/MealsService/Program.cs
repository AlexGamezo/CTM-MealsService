using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MealsService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5002")
                .Build();

            host.Run();
        }
    }
}
