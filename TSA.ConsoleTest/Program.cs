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
        static void Main(string[] args)
        {
            var topicGrouper = new TopicGrouper();
            var topics = topicGrouper.GroupDocuments( new JsonDocumentSource(), 10 );
            for( int i = 0; i < topics.Count; i++ ) {
                Console.WriteLine( $"Topic {i} - {topics[i].Documents.Count}" );
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

        private sealed class TestDocumentSource : IDocumentSource {
            public IEnumerable<IDocument> GetDocuments()
            {
                var random = new Random();

                char GetChar()
                {
                    return ( char ) ( 'a' + random.Next( 10 ) );
                }

                string GetWord()
                {
                    var s = new char[2];
                    for( var i = 0; i < s.Length; i++ ) {
                        s[ i ] = GetChar();
                    }

                    return new string( s );
                }

                string GetContent()
                {
                    return string.Join( ' ', GetWord(), GetWord(), GetWord() );
                }

                for ( var i = 0; i < 1000; i++ ) {
                    yield return new TestDocument( "Test", GetContent() );
                }
            }
        }

        public sealed class TestDocument : IDocument {
            public TestDocument() { }

            public TestDocument(
                string name,
                string content )
            {
                Name = name;
                Content = content;
            }
            [JsonProperty]
            public string Name { get; set; }
            [JsonProperty]
            public string Content { get; set; }
        }
    }
}
