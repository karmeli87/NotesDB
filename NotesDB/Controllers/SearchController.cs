using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NotesDB.Indexes;
using NotesDB.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace NotesDB.Controllers
{
    public class SearchController : HelperController
    {
        public async Task<IActionResult> Index()
        {
            return View(await GetAllTagTerms());
        }

        [HttpGet]
        public async Task<IActionResult> FindResults(string tags,string content)
        {
            var tagsArr = CleanTags(tags);
            
            using (var session = Store.OpenAsyncSession())
            {
                var results = session.Query<MedicalEntry, MedicalEntryIndexByTags>();
                if (tagsArr?.Length > 0)
                {
                    results = results.Where(x => x.Tags.ContainsAll(tagsArr));
                }
                if (string.IsNullOrEmpty(content) == false)
                {
                    results = results.Search(x=>x.Content,content);
                }
                return View(await results.OrderByDescending( e=> e.PostedOn).ToListAsync());
            }
        }
    }
}