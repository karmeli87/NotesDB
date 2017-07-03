using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NotesDB.Controllers;

namespace NotesDB.Models
{
    public class MedicalEntry
    {
        public class Image
        {
            public string Path;
            public string Extension;
            public int Size;
            public string Name;
            public string Type;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime PostedOn { get; set; }
        public string[] Tags { get; set; }
        public string Content { get; set; }
        public Dictionary<string,Image> Images = new Dictionary<string, Image>();

        public static MedicalEntry FromString(IFormCollection collection)
        {
            string id = null;
            if (string.IsNullOrEmpty(collection[nameof(Id)]) == false)
            {
                id = collection[nameof(Id)];
            }
            return new MedicalEntry
            {
                Id = id,
                Title = collection[nameof(Title)],
                Content = collection[nameof(Content)],
                PostedOn = DateTime.UtcNow,
                Tags = HelperController.CleanTags(collection[nameof(Tags)])
            };
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Content) || Tags == null || Tags.Length == 0)
            {
                throw new InvalidDataException();
            }
        }
    }
}
