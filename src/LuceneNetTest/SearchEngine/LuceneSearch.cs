#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using LuceneNetTest.Model;
using Version = Lucene.Net.Util.Version;

#endregion

namespace LuceneNet.SearchEngine
{
    /// <summary>
    ///     Class representing the Lucene search engine.
    /// </summary>
    public class LuceneSearch
    {
        #region Constants

        /// <summary>
        ///     Name of the Lucene index sub directory.
        /// </summary>
        public const String LuceneIndexName = "lucene_index";

        /// <summary>
        ///     Write lock constant.
        /// </summary>
        private const String WriteLock = "write.lock";

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the temporary <see cref="FSDirectory" />.
        /// </summary>
        private FSDirectory DirectoryTemp { get; set; }

        /// <summary>
        ///     Gets or sets the Lucene sub directory.
        /// </summary>
        public String LuceneDir { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="FSDirectory" />.
        /// </summary>
        private FSDirectory Directory
        {
            get
            {
                if ( DirectoryTemp == null )
                    DirectoryTemp = FSDirectory.Open( new DirectoryInfo( LuceneDir ) );
                if ( IndexWriter.IsLocked( DirectoryTemp ) )
                    IndexWriter.Unlock( DirectoryTemp );
                var lockFilePath = Path.Combine( LuceneDir, WriteLock );
                if ( File.Exists( lockFilePath ) )
                    File.Delete( lockFilePath );
                return DirectoryTemp;
            }
        }

        #endregion

        #region Private members

        /// <summary>
        ///     Adds the given <see cref="SampleData" /> to the index using the given writer.
        /// </summary>
        /// <param name="sampleData"><see cref="SampleData" /> to </param>
        /// <param name="writer"><see cref="IndexWriter" />.</param>
        private void AddToLuceneIndex( SampleData sampleData, IndexWriter writer )
        {
            // remove older index entry
            var searchQuery = new TermQuery( new Term( nameof( sampleData.Id ), sampleData.Id.ToString() ) );
            writer.DeleteDocuments( searchQuery );

            // add new index entry
            var doc = new Document();

            // Add Lucene fields mapped to db fields
            doc.Add( new Field( nameof( sampleData.Id ), sampleData.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED ) );
            doc.Add( new Field( nameof( sampleData.Name ), sampleData.Name, Field.Store.YES, Field.Index.ANALYZED ) );
            doc.Add( new Field( nameof( sampleData.Description ), sampleData.Description, Field.Store.YES, Field.Index.ANALYZED ) );

            // add entry to index
            writer.AddDocument( doc );
        }

        /// <summary>
        ///     Maps index to the <see cref="SampleData" />.
        /// </summary>
        /// <param name="doc"><see cref="Document" />.</param>
        /// <returns>Returns a <see cref="SampleData" /> object.</returns>
        private SampleData MapLuceneDocumentToData( Document doc ) => new SampleData
        {
            Id = Convert.ToInt32( doc.Get( nameof( SampleData.Id ) ) ),
            Name = doc.Get( nameof( SampleData.Name ) ),
            Description = doc.Get( nameof( SampleData.Description ) )
        };

        /// <summary>
        ///     Maps index to <see cref="SampleData" /> objects.
        /// </summary>
        /// <param name="hits">Collection of <see cref="Document" />.</param>
        /// <returns>Returns <see cref="SampleData" /> mappings.</returns>
        private IEnumerable<SampleData> MapLuceneToDataList( IEnumerable<Document> hits ) => hits.Select( MapLuceneDocumentToData )
                                                                                                 .ToList();

        /// <summary>
        ///     Maps index to <see cref="SampleData" /> objects.
        /// </summary>
        /// <param name="hits">Collection of <see cref="Document" />.</param>
        /// <param name="searcher"><see cref="IndexSearcher" />.</param>
        /// <returns>Returns <see cref="SampleData" /> mappings.</returns>
        private IEnumerable<SampleData> _mapLuceneToDataList( IEnumerable<ScoreDoc> hits,
                                                              IndexSearcher searcher ) => hits.Select( hit => MapLuceneDocumentToData( searcher.Doc( hit.Doc ) ) )
                                                                                              .ToList();

        #endregion

        #region Implementation of ILuceneSearch

        /// <summary>
        ///     Adds the given <see cref="IEnumerable{SampleData}" /> to the index.
        /// </summary>
        /// <remarks>Call this method whenever records are added or updated to the repository.</remarks>
        /// <param name="sampleDatas"><see cref="IEnumerable{SampleData}" /> to add.</param>
        public void AddUpdateLuceneIndex( IEnumerable<SampleData> sampleDatas )
        {
            // Initialize Lucene
            var analyzer = new StandardAnalyzer( Version.LUCENE_30 );
            using ( var writer = new IndexWriter( Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED ) )
            {
                // add data to Lucene search index (replaces older entry if any)
                foreach ( var sampleData in sampleDatas )
                    AddToLuceneIndex( sampleData, writer );

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }

        /// <summary>
        ///     Adds the given <see cref="SampleData" /> to the index.
        /// </summary>
        /// <remarks>Call this method whenever a record is added or updated to the repository.</remarks>
        /// <param name="sampleData"><see cref="SampleData" /> to add.</param>
        public void AddUpdateLuceneIndex( SampleData sampleData ) => AddUpdateLuceneIndex( new List<SampleData> { sampleData } );

        /// <summary>
        ///     Clears the index record with the given id.
        /// </summary>
        /// <remarks>Call this method whenever a record is deleted from the repository.</remarks>
        /// <param name="recordId">Record id.</param>
        public void ClearLuceneIndexRecord( Int32 recordId )
        {
            // Initialize Lucene
            var analyzer = new StandardAnalyzer( Version.LUCENE_30 );
            using ( var writer = new IndexWriter( Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED ) )
            {
                // remove older index entry
                var searchQuery = new TermQuery( new Term( nameof( SampleData.Id ), recordId.ToString() ) );
                try
                {
                    writer.DeleteDocuments( searchQuery );
                }
                finally
                {
                    // close handles
                    writer.Dispose();
                    analyzer.Close();
                }
            }
        }

        /// <summary>
        ///     Deletes all index records.
        /// </summary>
        /// <returns>Returns a value indicating whether the operation was successful.</returns>
        public Boolean ClearLuceneIndex()
        {
            try
            {
                var analyzer = new StandardAnalyzer( Version.LUCENE_30 );
                using ( var writer = new IndexWriter( Directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED ) )
                    try
                    {
                        // remove older index entries
                        writer.DeleteAll();
                    }
                    finally
                    {
                        // close handles
                        writer.Dispose();
                        analyzer.Close();
                    }
            }
            catch ( Exception )
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Optimizes the Lucene index.
        /// </summary>
        public void Optimize()
        {
            var analyzer = new StandardAnalyzer( Version.LUCENE_30 );
            using ( var writer = new IndexWriter( Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED ) )
            {
                analyzer.Close();
                try
                {
                    writer.Optimize();
                }
                finally
                {
                    writer.Dispose();
                }
            }
        }

        #endregion
    }
}