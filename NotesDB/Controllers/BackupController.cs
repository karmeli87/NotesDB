using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Operations.OngoingTasks;
using Raven.Client.Documents.Smuggler;

namespace NotesDB.Controllers
{
    public static class Backup
    {
        public static readonly string LocalRootFolder = Environment.GetEnvironmentVariable("LocalAppData");
        public static readonly long TaskId = 123;
    }
    public class BackupController : RavenController
    {
        private static Task _authTask;
        public IActionResult Index()
        {
            if (_authTask == null || _authTask.IsCompleted)
            {
                _authTask = GoogleApi.TryAuthenticateAsync();
            }
            return View();
        }

        [HttpGet]
        public async Task<string> LocalBackup()
        {
            await Store.Maintenance.SendAsync(new StartBackupOperation(false, Backup.TaskId));

            var backupStatus = new GetPeriodicBackupStatusOperation(Backup.TaskId);
            var result = await Store.Maintenance.SendAsync(backupStatus);
            var path = Path.Combine(Backup.LocalRootFolder, result.Status.FolderName);
          //  var file = Directory.GetFiles(path).Last();
            return path;
        }

        [HttpGet]
        public async Task CloudBackup(string path)
        {
            await _authTask;
            await GoogleApi.Instance.Upload(path);
        }
    }
}