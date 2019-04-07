using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotesDB.Controllers;
using NotesDB.Indexes;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Operations.OngoingTasks;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace NotesDB
{
    public class Startup
    {
        public static IDocumentStore Store;
        public static string Url = "";
        public static string DbName = "";
        public static string Api = "";
        public static string AppName = "";

        public static async Task InitializeDb()
        {
            Store = EmbeddedServer.Instance.GetDocumentStore(DbName);
            //Store.Initialize();
            try
            {
                await Store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(DbName)));
            }
            catch
            {
                // ignore
            }

            try
            {
                var ongoingTasksInfo = new GetOngoingTaskInfoOperation(Backup.TaskId, OngoingTaskType.Backup);
                var taskInfo = await Store.Maintenance.ForDatabase(DbName).SendAsync(ongoingTasksInfo);
                if (taskInfo == null)
                {
                    var config = new PeriodicBackupConfiguration()
                    {
                        LocalSettings = new LocalSettings
                        {
                            FolderPath = Backup.LocalRootFolder
                        },
                        FullBackupFrequency = "0 2 * * *",
                        BackupType = BackupType.Backup,
                        Name = "BackupTask",
                        TaskId = Backup.TaskId,
                    };
                    var operation = new UpdatePeriodicBackupOperation(config);
                    await Store.Maintenance.SendAsync(operation);
                }
            }
            catch
            {
                // ignore
            }

            var index = new MedicalEntryIndexByTags();
            var index2 = new MedicalEntryTagCounterIndex();

            index.Execute(Store);
            index2.Execute(Store);
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

            EmbeddedServer.Instance.StartServer(new ServerOptions
            {
                AcceptEula = true,
                ServerUrl = Url,
                DataDirectory = @"C:\Work\NotesDB\RavenData"
            });

            InitializeDb().ContinueWith(_ =>
            {
                var config = (ConfigurationRoot) o;
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
