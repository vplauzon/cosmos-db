using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace PushDocs
{
    class Program
    {
        #region DB Connections
        private const string SERVICE_ENDPOINT = "https://vpl.documents.azure.com:443/";
        private const string AUTH_KEY = "unFCqdUzrb3QUIk2JHeimJznnzmwnt8DOw5TGRsueudmoNSCFCtrttIM7j773qPhQn67agAnd9ottAPHNplDow==";
        private const string DATABASE = "mydb";
        private const string COLLECTION = "mycoll";
        #endregion

        static void Main(string[] args)
        {
            InsertDocumentsAsync().Wait();
        }

        private static async Task InsertDocumentsAsync()
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), AUTH_KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DATABASE, COLLECTION);
            var james = GetEmbeddedResource("Edge-James.json");
            var laura = GetEmbeddedResource("Edge-Laura.json");
            var phil = GetEmbeddedResource("Edge-Phil.json");

            foreach (var obj in new[] { james, laura, phil })
            {
                await client.CreateDocumentAsync(
                    collectionUri,
                    obj,
                    new RequestOptions { PreTriggerInclude = new[] { "formatEdge" } });
            }

            Console.WriteLine(james);
        }

        private static object GetEmbeddedResource(string name)
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;

            using (var stream = assembly.GetManifestResourceStream("PushDocs." + name))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                var obj = serializer.Deserialize(jsonReader);

                return obj;
            }
        }
    }
}