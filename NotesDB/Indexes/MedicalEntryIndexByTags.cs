using System.Linq;
using NotesDB.Models;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq.Indexing;

namespace NotesDB.Indexes
{
    public class MedicalEntryIndexByTags : AbstractIndexCreationTask<MedicalEntry>
    {
        public MedicalEntryIndexByTags()
        {
            Map = results => from entry in results
                select new
                {
                    entry.Tags,
                    entry.Title,
                    entry.PostedOn,
                    Content = entry.Content.StripHtml()
                };
            Indexes.Add(x=>x.Id,FieldIndexing.NotAnalyzed);
            Indexes.Add(x=>x.Content,FieldIndexing.Analyzed);
            //Analyzers.Add(x=>x.Content, typeof(RussianAnalyzer).AssemblyQualifiedName);
        }
    }

    public class MedicalEntryTagCounterIndex : AbstractIndexCreationTask<MedicalEntry, MedicalEntryTagCounterIndex.Result>
    {
        public class Result
        {
            public string Tag;
            public int Count;
        }

        public MedicalEntryTagCounterIndex()
        {
            Map = results => from entry in results
                from tag in entry.Tags
                select new
                {
                    Tag = tag,
                    Count = 1,
                };

            Reduce = results => from result in results
                group result by result.Tag
                into g
                select new
                {
                    Tag = g.Key,
                    Count = g.Sum(x => x.Count),
                };
        }
    }
    
}

