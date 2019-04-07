using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace NotesDB
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(o =>
                {
                    o.Listen(IPAddress.Parse("192.168.1.217"),5000, listenOptions =>
                        {
                            listenOptions.UseHttps(@"C:\Users\Karmel\Downloads\RavenDB-4.1.4-windows-x64\Server\cluster.server.certificate.medicaldb.pfx");
                        });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                //.UseUrls($"http://{Environment.MachineName}:5000")
                .Build();

            host.Run();
        }
    }
}
