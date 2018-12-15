using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Legacy;
using Microsoft.ML.Legacy.Data;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Transforms.Text;

using TSA.Interfaces;

namespace TSA.ML
{
    public class Trainer
    {
        public void Run(
            IDocumentSource source )
        {
            var context = new MLContext();
            var environment = context.Data.GetEnvironment();

            var schema = SchemaDefinition.Create( typeof( IDocument ), SchemaDefinition.Direction.Read );
            var data = environment.CreateStreamingDataView( source.GetDocuments(), schema );

            var featurize = context.Transforms.Text.FeaturizeText(
                "Content",
                "TextFeatures",
                s => {
                    s.KeepPunctuations = false;
                    s.KeepNumbers = false;
                    s.OutputTokens = true;
                    s.TextCase = TextNormalizingEstimator.CaseNormalizationMode.Lower;
                    s.TextLanguage = TextFeaturizingEstimator.Language.English;
                } );
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
            var ngram = new NgramExtractingEstimator( context, "WordTokens", "Ngrams", weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf );
            var wordBag = context.Transforms.Text.ProduceWordBags(
                "WordTokens",
                "BagOfWords",
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf );
            var normVec = context.Transforms.Projection.LpNormalize( "BagOfWords", "Features" );
            var transform = featurize.Append( normalize ).Append( tokenize ).Append( stopWords ).Append( wordBag );
            var transformedData = transform.Fit( data ).Transform( data );
            //var n = transformedData.GetColumn<float[]>( context, "BagOfWords" ).ToList();
            var clustering = context.Clustering.Trainers.KMeans("TextFeatures", clustersCount: 10 );

            var pipeline = transform.Append( clustering );
            var model = pipeline.Fit( data );
            context.Model.Save( model, File.Create( "model.dat" ) );

            //var embeddings = transformedData.GetColumn<float[]>(context, "Embeddings").Take(10).ToArray();
            //var unigrams = transformedData.GetColumn<float[]>(context, "BagOfWords").Take(10).ToArray();

            var prediction = model.MakePredictionFunction<IDocument, PredictionResult>( context );

            var results = new List<PredictionResult>();
            foreach( var document in source.GetDocuments().Take( 25 ) ) {
                var result = prediction.Predict( document );
                result.Name = document.Name;
                results.Add( result );
            }

            foreach( var result in results.OrderBy( x => x.PredictedLabel ) ) {
                Console.WriteLine( $"{result.PredictedLabel} - {result.Name}" );
            }
        }

        /*
        .Append( context.Transforms.Text.TokenizeCharacters( "Content", "ContentChars" ) )
        .Append(
            new NgramExtractingEstimator(
                context,
                "ContentChars",
                "BagOfTrichar",
                ngramLength: 3,
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf ) )
        .Append( context.Transforms.Text.TokenizeWords( "NormalizedContent", "TokenizedContent" ) ).Append(
            context.Transforms.Text.ExtractWordEmbeddings(
                "TokenizedContent",
                "Embeddings",
                WordEmbeddingsExtractingTransformer.PretrainedModelKind.GloVeTwitter25D ) );*/
    }

    public class PredictionResult
    {
        public uint PredictedLabel { get; set; }
        [VectorType(10)]
        public float[] Score { get; set; }
        public string Name { get; set; }
    }
}
