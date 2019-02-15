using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotesDB.Models;
using Raven.Client;
using Raven.Client.Documents.Operations.Attachments;
using Raven.Client.Documents.Session;

namespace NotesDB.Controllers
{
    public class TextEditorController : RavenController
    {

        // GET: TextEditor
        public ActionResult Index(MedicalEntry model)
        {
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            try
            {
                using (var session = Store.OpenAsyncSession())
                {
                    var entry = await session.LoadAsync<MedicalEntry>(id);
                    entry.Id = id;

                    var names = session.Advanced.Attachments.GetNames(entry);
                    if (names.Length > 0)
                    {
                        if (entry.Images == null)
                        {
                            entry.Images = new Dictionary<string, MedicalEntry.Image>();
                        }
                        LoadAttachments(names, entry);
                    }
                    return View("Index",entry);
                }
            }
            catch (Exception e)
            {
                return View("Error");
            }
        }

        private void LoadAttachments(AttachmentName[] names, MedicalEntry entry)
        {
            foreach (var attachment in names)
            {
                var name = attachment.Name;
                var url = $"{Store.Urls[0]}/databases/{Store.Database}/attachments?id={Uri.EscapeDataString(entry.Id)}&name={Uri.EscapeDataString(name)}";

                entry.Images[name] = new MedicalEntry.Image
                {
                    Name = Regex.Escape(name),
                    Type = attachment.ContentType,
                    Size = (int)attachment.Size,
                    Path = url
                };
            }
        }
    
        // POST: TextEditor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            MedicalEntry entity = new MedicalEntry();
            
            try
            {
                entity = MedicalEntry.FromString(collection);
                var id = entity.Id;
                entity.Validate();
                List<Stream> streams = new List<Stream>();

                using (var session = Store.OpenAsyncSession())
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        await session.StoreAsync(entity);
                        entity.Id = session.Advanced.GetDocumentId(entity);
                        streams.AddRange(SaveImages(session, collection, entity));
                        await session.SaveChangesAsync();
                    }
                    else
                    {
                        var existing = await session.LoadAsync<MedicalEntry>(id);
                        existing.Tags = entity.Tags;
                        existing.Content = entity.Content;
                        existing.PostedOn = entity.PostedOn;
                        existing.Title = entity.Title;

                        var metadata = session.Advanced.GetMetadataFor(existing);
                        metadata.Remove(Constants.Documents.Metadata.Attachments);
                        streams.AddRange(SaveImages(session, collection, existing));
                        await session.SaveChangesAsync();
                    } 
                }

                foreach (var stream in streams)
                {
                    stream.Dispose();
                }

                return RedirectToAction("Index");
            }
            catch(Exception e)
            {
                return View("Index",entity);
            }
        }

        private static List<Stream> SaveImages(IAsyncDocumentSession session, IFormCollection collection, MedicalEntry entity)
        {
            var streams = new List<Stream>();
            if (collection.Files != null && collection.Files.Count > 0)
            {
                foreach (var file in collection.Files)
                {
                    if (string.IsNullOrEmpty(file.FileName))
                        continue;

                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    session.Advanced.Attachments.Store(entity.Id,file.FileName,stream,file.ContentType);
                }
            }
            return streams;
        }

        // GET: TextEditor/Delete
        public async Task<string> DeleteAttachment(IFormCollection collection)
        {
            var name = collection["name"];
            var id = collection["key"];
            using (var session = Store.OpenAsyncSession())
            {
                session.Advanced.Attachments.Delete(id,name);
                await session.SaveChangesAsync();
            }
            return "{}";
        }
    }
}