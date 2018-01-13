using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotesDB.Indexes;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace NotesDB
{
    public class Startup
    {
        public static DocumentStore Store;
        public static string Url = "";
        public static string DbName = "";
        public static string Api = "";
        public static string AppName = "";

        public static async Task InitializeDb()
        {
            try
            {
                Store = new DocumentStore
                {
                    Database = DbName,
                    Urls = new []{Url},
                };
                Store.Initialize();
                await Store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(DbName)));

                var index = new MedicalEntryIndexByTags();
                var index2 = new MedicalEntryTagCounterIndex();
                index.Execute(Store);
                index2.Execute(Store);
            }
            catch (Exception e)
            {
                //
            }
        }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("settings.json",optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            LoadConfig(Configuration);
        }

        private void LoadConfig(object o)
        {
            Url = Configuration["url"];
            DbName = Configuration["db"];
            Api = Configuration["google-api-key"];
            AppName = Configuration["google-app"];

            InitializeDb().ContinueWith(_ =>
            {
                var config = (ConfigurationRoot)o;
                config.GetReloadToken().RegisterChangeCallback(LoadConfig, o);
            });            
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
               // app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });

            applicationLifetime.ApplicationStopping.Register(Stop);

            
        }

        public void Stop()
        {
            Store?.Dispose();
        }
    }
}
