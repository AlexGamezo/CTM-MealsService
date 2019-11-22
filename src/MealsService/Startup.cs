using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using MealsService.Configurations;
using MealsService.Recipes;
using MealsService.Diets;
using MealsService.Diets.Data;
using MealsService.Email;
using MealsService.Images;
using MealsService.Infrastructure;
using MealsService.Ingredients;
using MealsService.Schedules;
using MealsService.ShoppingList;
using MealsService.Tags;
using MealsService.Stats;
using MealsService.Users;
using MealsService.Common.Errors;
using MealsService.Notifications;

namespace MealsService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddSystemsManager($"/Meals/{env.EnvironmentName}")
                .AddSystemsManager($"/Shared/{env.EnvironmentName}")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var connection = Configuration.GetConnectionString("MySQL");
            services.AddDbContext<MealsDbContext>(options => options.UseMySql(connection));

            services.AddEnyimMemcached();
            services.AddMemoryCache();

            // Add framework services.
            services.AddMvc();
            //services.AddScoped<RecipesService>();
            services.AddScoped<IRecipesService, RecipesService>();
            services.AddScoped<IUserRecipesService, UserRecipesService>();
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IUserRecipeRepository, UserRecipeRepository>();

            services.AddScoped<ScheduleService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<ScheduleRepository>();
            
            services.AddScoped<IIngredientsService, IngredientsService>();
            services.AddScoped<IIngredientsRepository, IngredientsRepository>();
            
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ITagsService, TagsService>();
            
            services.AddScoped<DietTypeService>();
            services.AddScoped<DietService>();
            services.AddScoped<DietsRepository>();
            
            services.AddScoped<SubscriptionsService>();
            
            services.AddScoped<IImageService, ImageService>();

            services.AddScoped<IShoppingListService, ShoppingListService>();
            services.AddScoped<ShoppingListRepository>();
            
            services.AddScoped<StatsService>();
            
            services.AddScoped<UsersService>();
            
            services.AddScoped<RequestContext>();
            services.AddScoped<RequestContextFactory>();

            services.AddScoped<INotificationsService, NotificationsService>();

            services.AddScoped<EmailService>();
            
            services.AddScoped<IViewRenderService, ViewRenderService>();
            
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();
            services.Configure<AWSConfiguration>(Configuration.GetSection("AWS"));
            
            services.Configure<SendgridConfiguration>(Configuration.GetSection("Sendgrid"));
            services.Configure<ServicesConfiguration>(Configuration.GetSection("Services"));
            
            services.Configure<CredentialsConfiguration>(Configuration.GetSection("Credentials"));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // secretKey contains a secret passphrase only your server knows
                    var secretKey = Configuration.GetValue<string>("Authentication:AccessTokenKey");
                    var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateIssuer = true,
                        ValidIssuer = Configuration.GetValue<string>("Authentication:Issuer"),
                        ValidateAudience = true,
                        ValidAudience = Configuration.GetValue<string>("Authentication:Audience"),
                        ValidateLifetime = true,
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = "role"
                    };
                });

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            app.UseDeveloperExceptionPage();
            app.UseMiddleware<JsonExceptionHandlerMiddleware>();
            app.UseMiddleware<RequestContextHydrateMiddleware>();
            app.UseEnyimMemcached();

            app.UseCors(builder =>
            {
                builder.WithOrigins(
                        //Web local run host
                        "http://localhost:5000",
                        //Admin local run host
                        "http://localhost:5003",
                        "https://www.greenerplate.com/"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    ;
            });

            app.UseAuthentication();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
