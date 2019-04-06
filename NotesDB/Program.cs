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
                    o.Listen(IPAddress.Loopback,5000);
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
