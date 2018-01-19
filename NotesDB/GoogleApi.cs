﻿using System;
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
    public static class GoogleApi
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        private static readonly string[] Scopes = { DriveService.Scope.Drive };
             
        private static DriveService _service;
        
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


        public static async Task Upload(string path)
        {
            if (_service == null)
            {
                throw new AuthenticationException("Service is null");
            }

            var fn = Path.GetFileName(path);
            var id = await FindId(fn);

            GoogleFile fileToUpload = new GoogleFile
            {
                Name = fn,
                MimeType = "application/octet-stream",
            };
            var fileInfo = new FileInfo(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                ResumableUpload request;
                if (id == null)
                {
                    request = _service.Files.Create(fileToUpload, stream, "application/octet-stream");
                }
                else
                {
                    request = _service.Files.Update(fileToUpload, id, stream, "application/octet-stream");
                }
                await Upload(request, fileInfo);
            }
        }

        private static async Task Upload(ResumableUpload request, FileInfo fileInfo)
        {
            while (true)
            {
                IUploadProgress res = null;
                try
                {
                    res = await request.ResumeAsync();
                }
                catch (TaskCanceledException)
                {
                    //Console.WriteLine($"Sent {res?.BytesSent ?? 0 / fileInfo.Length * 100} %");
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

        public static async Task<string> FindId(string name)
        {
            var request = _service.Files.List();
            request.Spaces = "drive";
            request.Q = $"name = '{name}'";
            request.Fields = "files(id)";
            var result = await request.ExecuteAsync();
            return result.Files.FirstOrDefault()?.Id;
        }
    }
}
