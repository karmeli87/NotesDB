using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NotesDB.Indexes;
using Raven.Client.Documents;

namespace NotesDB.Controllers
{
    public class RavenController : Controller
    {
        public static DocumentStore Store = Startup.Store;
        
        public async Task<IEnumerable<string>> GetTagTerms(string startingWith, int pageSize)
        {
            using (var session = Store.OpenAsyncSession())
            {
                return await session.Query<MedicalEntryTagCounterIndex.Result, MedicalEntryTagCounterIndex>()
                    .Where(x=>x.Tag.StartsWith(startingWith))
                    .OrderByDescending(x => x.Count)
                    .Take(pageSize).Select(x => x.Tag).ToListAsync();
            }
      //      var indexName = "MedicalEntryTagCounterIndex";
      //      return await Store.Admin.ForDatabase(DbName).SendAsync(new GetTermsOperation(indexName, "Tag", startingWith, pageSize));
        }
        
    }
}
