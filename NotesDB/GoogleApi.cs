using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Security;
using GoogleFile = Google.Apis.Drive.v3.Data.File;
namespace NotesDB
{
    public class GoogleApi
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        private static readonly string[] Scopes = { DriveService.Scope.Drive };
             
        private static DriveService _service;

        public static readonly GoogleApi Instance;

        static GoogleApi()
        {
            Instance = new GoogleApi();
        }

        public static async Task TryAuthenticateAsync()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret_api.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetEnvironmentVariable("LocalAppData");
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            // Create Drive API service.
            _service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Startup.AppName,
                ApiKey = Startup.Api
            });
        }

        private class FileUploadStatus
        {
            public long TotalSize;
            public long PrevSent;
            public Stopwatch Stopwatch;
        }

        public async Task Upload(string path)
        {
            if (_service == null)
            {
                throw new AuthenticationException("Service is null");
            }

            var folderId = await GetFolder($"{DateTime.Today.ToShortDateString()}");
            var existingFiles = await LastFile(folderId);

            foreach (var filePath in Directory.GetFiles(path))
            {
                var fn = new FileInfo(filePath);
                if (existingFiles.Contains(fn.Name))
                    continue;

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var request = PrepareUploadRequest(filePath, folderId, stream);

                    _fileUploadStatus = new FileUploadStatus
                    {
                        TotalSize = fn.Length
                    };

                    await Upload(request);
                }
            }

            
        }

        private async Task<List<string>> LastFile(string folderId)
        {
            var listRequest = _service.Files.List();
            listRequest.OrderBy = "name";
            listRequest.Q = $"'{folderId}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed=false";
            //listRequest.Fields = "files(id)";

            var result = await listRequest.ExecuteAsync();
            return result.Files.Select(x => x.Name).ToList();
        }

        private FilesResource.CreateMediaUpload PrepareUploadRequest(string path, string id, FileStream stream)
        {
            var fn = Path.GetFileName(path);
            var fileMetadata = new GoogleFile
            {
                Name = fn,
                Parents = new List<string>
                {
                    id
                }
            };
            var fileRequest = _service.Files.Create(fileMetadata, stream, "application/json");
            fileRequest.ChunkSize = ResumableUpload.MinimumChunkSize * 4; // 1MB
            fileRequest.ProgressChanged += ShowProgress;

            fileRequest.Fields = "id";
            return fileRequest;
        }

        private FileUploadStatus _fileUploadStatus;

        private async Task Upload(ResumableUpload request)
        {
            _fileUploadStatus.Stopwatch = Stopwatch.StartNew();
            while (true)
            {
                IUploadProgress res;
                try
                {
                    res = await request.ResumeAsync();
                }
                catch (TaskCanceledException)
                {
                    continue;
                }

                if (res.Status == UploadStatus.Completed)
                    break;

                if (res.Status == UploadStatus.Failed)
                {
                    throw new BadRequestException(res.Exception.Message);
                }
            }
        }

        private void ShowProgress(IUploadProgress progress)
        {
            var elapsed = _fileUploadStatus.Stopwatch.Elapsed.TotalSeconds;
            _fileUploadStatus.Stopwatch = Stopwatch.StartNew();

            var sent = progress?.BytesSent ?? 0;
            var left = _fileUploadStatus.TotalSize - sent;
            var delta = (sent - _fileUploadStatus.PrevSent);
            var speed = delta / elapsed;
            _fileUploadStatus.PrevSent = sent;

            Console.WriteLine(
                $"Sent {sent * 100 / _fileUploadStatus.TotalSize:N} %, {(speed / (1024 * 1024)):N} MB/s, done in approx. {left / speed:N} sec");
        }

        private static string _rootFolderId;

        private static async Task<string> GetFolder(string name)
        {
            if (_rootFolderId == null)
            {
                var rootId = await TryFindFolder("NotesDB") ;
                _rootFolderId = rootId ?? throw new InvalidOperationException("Root folder 'NotesDB' not found.");
            }

            var id = await TryFindFolder(name, _rootFolderId);
            if (id != null)
                return id;

            var result = await CreateNewFolder(name);
            return result.Id;
        }

        private static async Task<GoogleFile> CreateNewFolder(string name)
        {
            var folder = new GoogleFile
            {
                Name = name,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string>
                {
                    _rootFolderId
                }
            };

            var folderRequest = _service.Files.Create(folder);
            folderRequest.Fields = "id";
            return await folderRequest.ExecuteAsync();
        }

        private static async Task<string> TryFindFolder(string path, string parent = "Root")
        {
            var listRequest = _service.Files.List();
            listRequest.Fields = "files(id)";

            listRequest.Q = $"'{parent}' in parents and mimeType = 'application/vnd.google-apps.folder' and name = '{path}'";
            var result = await listRequest.ExecuteAsync();
            return result.Files.SingleOrDefault()?.Id;
        }
    }
}
