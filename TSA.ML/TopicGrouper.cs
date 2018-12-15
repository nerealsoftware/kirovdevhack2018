using System.Collections.Generic;
using System.Linq;

using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Transforms.Text;

using TSA.Interfaces;

namespace TSA.ML
{
    public class TopicGrouper : ITopicGrouper
    {
        public IReadOnlyList<ITopic> GroupDocuments(
            IDocumentSource source,
            int numberOfGroups )
        {
            var context = new MLContext();
            var environment = context.Data.GetEnvironment();

            var schema = SchemaDefinition.Create( typeof( IDocument ), SchemaDefinition.Direction.Read );
            var data = environment.CreateStreamingDataView( source.GetDocuments(), schema );

            var normalize = context.Transforms.Text.NormalizeText(
                "Content",
                "NormalizedContent",
                TextNormalizingEstimator.CaseNormalizationMode.Lower,
                false,
                false,
                false );
            var tokenize = context.Transforms.Text.TokenizeWords(
                "NormalizedContent",
                "WordTokens",
                new[] {' ', '\n', '\r', '\t'} );
            var stopWords = context.Transforms.Text.RemoveStopWords(
                "WordTokens",
                language: StopWordsRemovingEstimator.Language.Russian );
            var wordEmbeddings = context.Transforms.Text.ExtractWordEmbeddings(
                "WordTokens",
                "WordEmbeddings",
                WordEmbeddingsExtractingTransformer.PretrainedModelKind.GloVe50D );
            var wordBag = context.Transforms.Text.ProduceWordBags(
                "WordTokens",
                "BagOfWords",
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf );
            var transform = normalize.Append( tokenize ).Append( stopWords ).Append( wordBag );
            var lda = context.Transforms.Text.LatentDirichletAllocation( "BagOfWords", "LDA", numberOfGroups );

            var pipeline = transform.Append( lda );
            var model = pipeline.Fit( data );
            var transformedData = model.Transform( data );
            var ldaData = transformedData.GetColumn<float[]>( context, "LDA" ).ToList();

            var topics = new List<Topic>( numberOfGroups );
            for( var i = 0; i < numberOfGroups; i++ ) {
                topics.Add( new Topic() );
            }

            var results = source.GetDocuments().Zip(
                ldaData,
                (
                    d,
                    s ) => new {
                    Document = d,
                    Scores = s
                } );
            foreach( var result in results ) {
                var maxScore = float.MinValue;
                var maxIndex = 0;
                for( var i = 0; i < numberOfGroups; i++ ) {
                    if( result.Scores[ i ] > maxScore ) {
                        maxScore = result.Scores[ i ];
                        maxIndex = i;
                    }

                    topics[ maxIndex ].Add( result.Document );
                }
            }

            return topics;
        }

        private class Topic : ITopic
        {
            private readonly List<IDocument> _documents;

            public Topic()
            {
                _documents = new List<IDocument>();
            }

            public void Add(
                IDocument document )
            {
                _documents.Add( document );
            }

            public IReadOnlyList<IDocument> Documents => _documents;
        }
    }
}