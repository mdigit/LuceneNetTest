#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using LuceneNetTest.Model;

#endregion

namespace LuceneNetTest.Dal
{
    public class SampleDataRepository
    {
        public SampleData Get( Int32 id ) => GetAll()
            .SingleOrDefault( x => x.Id.Equals( id ) );

        public List<SampleData> GetAll() => new List<SampleData>
        {
            new SampleData { Id = 1, Name = "Belgrad", Description = "City in Serbia" },
            new SampleData { Id = 2, Name = "Moscow", Description = "City in Russia" },
            new SampleData { Id = 3, Name = "Chicago", Description = "City in USA" },
            new SampleData { Id = 4, Name = "Mumbai", Description = "City in India" },
            new SampleData { Id = 5, Name = "Hong-Kong", Description = "City in Hong-Kong" }
        };
    }
}