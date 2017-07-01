
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Amazon.S3;
using MealsService.Configurations;
using MealsService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MealsService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var connection = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<MealsDbContext>(options => options.UseMySql(connection));

            // Add framework services.
            services.AddMvc();
            services.AddScoped<Services.RecipesService>();
            services.AddScoped<Services.ScheduleService>();
            services.AddScoped<Services.DietService>();
            services.AddScoped<Services.IngredientsService>();
            services.AddScoped<Services.DietTypeService>();

            services.Configure<AWSConfiguration>(Configuration.GetSection("AWS"));
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseDeveloperExceptionPage();

            // secretKey contains a secret passphrase only your server knows
            var secretKey = Configuration.GetValue<string>("Authentication:AccessTokenKey");
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

            app.UseCors(builder =>
            {
                builder.WithOrigins("http://localhost:63516")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = Configuration.GetValue<string>("Authentication:Issuer"),
                ValidateAudience = true,
                ValidAudience = Configuration.GetValue<string>("Authentication:Audience"),
                ValidateLifetime = true,
                NameClaimType = "sub",
                RoleClaimType = "role"
            };

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
