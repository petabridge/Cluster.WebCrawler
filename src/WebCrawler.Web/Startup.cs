// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IServiceProvider Provider { get; private set; }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSignalR();
            services.AddSingleton<CrawlHubHelper, CrawlHubHelper>();

            // creates an instance of the ISignalRProcessor that can be handled by SignalR
            services.AddSingleton<ISignalRProcessor, AkkaService>();

            // starts the IHostedService, which creates the ActorSystem and actors
            services.AddTransient<IHostedService, AkkaService>(sp => (AkkaService)sp.GetRequiredService<ISignalRProcessor>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(ep => {
                ep.MapControllerRoute("default",
                    "{controller=Home}/{action=Index}/{id?}");
                ep.MapHub<CrawlHub>("/hubs/crawlHub");
            });
        }
    }
}
