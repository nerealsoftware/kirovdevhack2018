using System;
using System.Collections.Generic;
using System.Linq;

using TSA.Interfaces;
using TSA.ML;

namespace TSA.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var trainer = new Trainer();
            trainer.Run( new TestDocumentSource() );
            Console.ReadLine();
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

        private sealed class TestDocument : IDocument {
            public TestDocument(
                string name,
                string content )
            {
                Name = name;
                Content = content;
            }
            public string Name { get; }
            public string Content { get; }
        }
    }
}
