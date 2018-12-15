using System;
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
            var data = BuildDataView( context, source );
            var pipeline = BuildPipeline( context, numberOfGroups );

            var model = pipeline.Fit( data );

            var transformedData = model.Transform( data );
            var ldaData = transformedData.GetColumn<float[]>( context, "LDA" ).ToList();

            var topics = BuildTopics( source, numberOfGroups, ldaData );
            return topics;
        }

        private static IDataView BuildDataView(
            MLContext context,
            IDocumentSource source )
        {
            var environment = context.Data.GetEnvironment();
            var schema = SchemaDefinition.Create( typeof( IDocument ), SchemaDefinition.Direction.Read );
            var data = environment.CreateStreamingDataView( source.GetDocuments(), schema );
            return data;
        }

        private static EstimatorChain<LatentDirichletAllocationTransformer> BuildPipeline(
            MLContext context,
            int numberOfGroups )
        {
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
            var wordBag = context.Transforms.Text.ProduceWordBags(
                "WordTokens",
                "BagOfWords",
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf );
            var lda = context.Transforms.Text.LatentDirichletAllocation( "BagOfWords", "LDA", numberOfGroups );
            var pipeline = normalize.Append( tokenize ).Append( stopWords ).Append( wordBag ).Append( lda );
            return pipeline;
        }

        private static List<Topic> BuildTopics(
            IDocumentSource source,
            int numberOfGroups,
            List<float[]> ldaData )
        {
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
                }

                topics[ maxIndex ].Add( result.Document, maxScore );
            }

            return topics;
        }

        private sealed class Topic : ITopic
        {
            private readonly List<ValueTuple<IDocument, float>> _documents;

            public Topic()
            {
                _documents = new List<ValueTuple<IDocument, float>>();
            }

            public void Add(
                IDocument document,
                float value )
            {
                _documents.Add( ValueTuple.Create( document, value ) );
            }

            public IReadOnlyList<ValueTuple<IDocument, float>> Documents => _documents;
        }
    }
}