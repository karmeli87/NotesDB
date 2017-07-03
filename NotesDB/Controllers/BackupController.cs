using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Smuggler;

namespace NotesDB.Controllers
{
    public class BackupController : RavenController
    {
        public IActionResult Index()
        {
            GoogleApi.TryAuthenticateAsync();
            return View();
        }

        [HttpGet]
        public async Task<string> LocalBackup()
        {
            return await Backup(DatabaseItemType.Documents);
        }

        [HttpGet]
        public async Task CloudBackup(string path)
        {
            await GoogleApi.Upload(path);
        }

        public static async Task<string> Backup(DatabaseItemType type)
        {
            string path = Environment.GetEnvironmentVariable("LocalAppData");
            var fileName = Path.Combine(path, type + "_Backup.ravendbdump");
            await Store.Smuggler.ExportAsync(new DatabaseSmugglerOptions
            {
                Database = DbName,
                OperateOnTypes = type,
            }, fileName);

            return fileName;
        }

    }
}