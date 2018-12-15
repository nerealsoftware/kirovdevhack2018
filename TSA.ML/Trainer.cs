using System;
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

            var documents = source.GetDocuments().Select(d => new Document(d));

            var schema = SchemaDefinition.Create( typeof( Document ), SchemaDefinition.Direction.Read );
            var data = environment.CreateStreamingDataView( documents, schema );

            var pipeline = context.Transforms.Text.FeaturizeText( "Content", "TextFeatures",
                    s => {
                        s.KeepPunctuations = false;
                        s.KeepNumbers = false;
                        s.TextCase = TextNormalizingEstimator.CaseNormalizationMode.Lower;
                        s.TextLanguage = TextFeaturizingEstimator.Language.English;
                    } )
                //.Append( context.Transforms.Text.NormalizeText( "Content", "NormalizedContent" ) )
                //.Append( new WordBagEstimator( context, "NormalizedContent", "BagOfWords" ) )
                //.Append( new WordHashBagEstimator( context, "NormalizedContent", "BagOfBigrams", ngramLength: 2, allLengths: false ) )
                .Append( context.Clustering.Trainers.KMeans( "TextFeatures", clustersCount: 10 ) );

            var model = pipeline.Fit( data );
            context.Model.Save( model, File.Create( "model.dat" ) );

            //var embeddings = transformedData.GetColumn<float[]>(context, "Embeddings").Take(10).ToArray();
            //var unigrams = transformedData.GetColumn<float[]>(context, "BagOfWords").Take(10).ToArray();

            var prediction = model.MakePredictionFunction<Document, PredictionResult>( context );

            var result = prediction.Predict( documents.First() );

            Console.ReadLine();
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

    public class Document : IDocument
    {
        private IDocument _document;
        public Document(
            IDocument document )
        {
            _document = document;
        }
        public int PredictionLabel { get; set; }
        public string Name => _document.Name;
        public string Content => _document.Content;
    }
    public class PredictionResult
    {
        public int PredictionLabel { get; set; }
    }
}
