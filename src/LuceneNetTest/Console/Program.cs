#region Usings

using System;
using System.IO;
using LuceneNet.SearchEngine;
using LuceneNetTest.Dal;

#endregion

namespace LuceneNetTest.TestAppConsole
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        #region Properties

        private static LuceneSearch LuceneSearch { get; } = new LuceneSearch();

        private static SampleDataRepository SampleDataRepository { get; } = new SampleDataRepository();

        #endregion

        private static String BuildAppPath() =>
            Path.Combine( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "LuceneNetTest" ), LuceneSearch.LuceneIndexName );

        private static void Main( String[] args )
        {
            Console.WriteLine( $"{System.AppDomain.CurrentDomain.FriendlyName} started." );
            // Test
            LuceneSearch.LuceneDir = BuildAppPath();
            Console.WriteLine( $"App path = {LuceneSearch.LuceneDir}" );
            LuceneSearch.AddUpdateLuceneIndex( SampleDataRepository.GetAll() );
            Console.WriteLine( "Hit any key to terminate" );
            Console.ReadKey();
        }
    }
}