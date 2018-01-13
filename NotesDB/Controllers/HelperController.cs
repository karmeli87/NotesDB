using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NotesDB.Controllers
{
    public class HelperController : RavenController
    {
        [HttpGet]
        //public async Task<ActionResult> LaizyTags(string startWith)
        public async Task<string[]> LaizyTags(string startWith)
        {
           // var startWith = Request.Query["startWith"].ToString();

            if (string.IsNullOrEmpty(startWith))
            {
                return await GetAllTagTerms(10);
            }

            var res = await GetTagTerms(startWith.ToLowerInvariant(), 10);
            //return all?.Where(t => t.StartsWith(startWith.ToLowerInvariant())).ToArray();
            //return Json(await GetTagTerms(startWith, 8));
            return res.ToArray();
        }

        public static string[] CleanTags(string tags)
        {
            return tags?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToArray();
        }

        public async Task<string[]> GetAllTagTerms(int pageSize = 16)
        {
            var result = await GetTagTerms("", pageSize);
           
            return result.ToArray();
        }
        
        [HttpGet]
        public async Task<FileStreamResult> ServeImage(string id, string name,string type)
        {
            using (var session = Store.OpenAsyncSession())
            {
                using (var res = await session.Advanced.Attachments.GetAsync(id, name))
                {
                    if (res == null)
                    {
                        throw new FileNotFoundException($"File {name} in entity {id} was not found");
                    }
                    return new FileStreamResult(res.Stream, string.IsNullOrEmpty(type) ? "image/jpeg" : type);
                }
            }
        }
    }
}
