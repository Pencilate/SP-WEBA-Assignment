using System;

using System.Text;


using TMS.Data;
using TMS.Helpers;

using TMS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;




namespace TMS
{
	public class Startup
	{

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
	   // This lambda determines whether user consent for non-essential cookies is needed for a given request.
	   options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			//---- Additional code added in Startup class's ConfigureServices
			//
			services.AddCors();//https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-2.2
			services.AddDbContext<ApplicationDbContext>(
			   options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
			);
			// Configure strongly typed settings objects
			var appSettingsSection = Configuration.GetSection("AppSettings");
			services.Configure<AppSettings>(appSettingsSection);

			// Configure jwt authentication
			var appSettings = appSettingsSection.Get<AppSettings>();
			// Before you can use the following command, var key = Encoding.ASCII.GetBytes(appSettings.Secret);
			// to obtain the secret key value can be read from the appsettings.json file, you need three
			// commands above to prepare the appSettings object.
			// Notice that an object of a custom type AppSettings has been defined inside
			// the Helpers namespace (folder).
			var key = Encoding.ASCII.GetBytes(appSettings.Secret);

			services.AddAuthentication(("CookieAuthenticationScheme"))
			  .AddCookie("CookieAuthenticationScheme", options =>
			   {
				  options.ExpireTimeSpan = TimeSpan.FromDays(7);
				  options.AccessDeniedPath = "/Home/Forbidden/";
				  options.LoginPath = "/Home/Login/";
			  }
			  );


			services.AddAntiforgery();
			//Configure DI for application services
			//https://stackoverflow.com/questions/38138100/what-is-the-difference-between-services-addtransient-service-addscoped-and-serv
			services.AddScoped<IUserService, UserService>();

			//The following code will create a singleton object inside the services collection so that
			//any Web API or Action controller class which uses this object can either create a current date time
			//or mock-up date time for testing purporses.
			//https://medium.com/@mattmazzola/asp-net-core-injecting-custom-data-classes-into-startup-classs-constructor-and-configure-method-7cc146f00afb
			//Declaration technique to apply current system date time
			services.AddSingleton<IAppDateTimeService>
			 (new AppDateTimeService("actual", DateTime.MinValue));

			//Declaration technique to apply mock up date time.
			// services.AddSingleton<IAppDateTimeService>(new AppDateTimeService("mock", new DateTime(2019, 7, 4, 11, 30, 0)));
			services.AddAuthorization(options =>
			{

				options.AddPolicy("ADMIN",
					authBuilder =>
					{
						authBuilder.RequireRole("ADMIN");
					});
				options.AddPolicy("INSTRUCTOR",
					authBuilder =>
					{
						authBuilder.RequireRole("INSTRUCTOR");
					});
				options.AddPolicy("PENDINGUSER",
                    authBuilder =>
                    {
	                    authBuilder.RequireRole("PENDINGUSER");
                    });
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}
			app.UseAuthentication();
			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCookiePolicy();
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
