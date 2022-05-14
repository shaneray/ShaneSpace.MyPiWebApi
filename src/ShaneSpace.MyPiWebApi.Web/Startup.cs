using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using ShaneSpace.MyPiWebApi.Services;
using ShaneSpace.MyPiWebApi.Web.MappingProfiles;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace ShaneSpace.MyPiWebApi.Web
{
    /// <summary>
    /// Application Startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Application Configuration Object
        /// </summary>
        private readonly IConfiguration _configuration;

        public static GpioService GpioService;

        public static MyPiService MyPiService { get; private set; }

        /// <summary>
        /// Application Startup Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Configures DI Container
        /// </summary>
        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist");

            // https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ShaneSpace MyPi Web API",
                    Description = "A simple example ASP.NET Core Web API on Raspberry PI",
                    TermsOfService = new Uri("http://shanespace.net/"),
                    Contact = new OpenApiContact
                    {
                        Name = "Shane Ray",
                        Email = string.Empty,
                        Url = new Uri("http://shanespace.net/"),
                    }
                });
            });

            RegisterApplicationServices(services);
        }

        /// <summary>
        /// Configure Application
        /// </summary>
        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyPi Web API V1");
                c.ConfigObject.AdditionalItems.Add("tryItOutEnabled", true);
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                //app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }
            //app.UseStaticFiles();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });

            var autoMapper = app.ApplicationServices.GetRequiredService<IMapper>();
            autoMapper.ConfigurationProvider.AssertConfigurationIsValid();

            app.ApplicationServices.GetRequiredService<IMyPiService>();

            Console.WriteLine($"Launched from {Environment.CurrentDirectory}");
            Console.WriteLine($"Physical location {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"AppContext.BaseDir {AppContext.BaseDirectory}");
            Console.WriteLine($"Runtime Call {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}");
        }

        private void RegisterApplicationServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_configuration);
            serviceCollection.AddSingleton(Log.Logger);

            serviceCollection.AddAutoMapper(typeof(ViewModelMappingProfile));
            serviceCollection.AddSingleton<IGpioService, GpioService>();
            serviceCollection.AddSingleton<IMyPiService, MyPiService>();
        }
    }
}
