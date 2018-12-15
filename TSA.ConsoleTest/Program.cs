using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using TSA.Interfaces;
using TSA.ML;

namespace TSA.ConsoleTest
{
    class Program
    {
        static void Main()
        {
            var topicGrouper = new TopicGrouper();
            var topics = topicGrouper.GroupDocuments( new JsonDocumentSource(), 20 );
            for( int i = 0; i < topics.Count; i++ ) {
                Console.WriteLine( $"Topic {i} - {topics[ i ].Documents.Count}" );
                foreach( var document in topics[ i ].Documents.Take( 5 ) ) {
                    Console.WriteLine( $"    {document.Item1.Name} ({document.Item2*100:F}%)" );
                }
            }

            Console.ReadLine();
        }

        private sealed class JsonDocumentSource : IDocumentSource
        {
            public IEnumerable<IDocument> GetDocuments()
            {
                var docs = JsonConvert.DeserializeObject<List<TestDocument>>(
                    File.ReadAllText( @"..\..\..\..\itmo.json" ) );
                return docs;
            }
        }

        public sealed class TestDocument : IDocument
        {
            [ JsonProperty ] public string Name { get; set; }
            [ JsonProperty ] public string Content { get; set; }
        }
    }
}