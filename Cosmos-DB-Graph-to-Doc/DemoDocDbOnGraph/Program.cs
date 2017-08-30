using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DemoDocDbOnGraph
{
    class Program
    {
        #region Inner Types
        private class MinimalDoc
        {
            public string id { get; set; }
            public bool? _isEdge { get; set; }
        }
        #endregion

        private const string SERVICE_ENDPOINT =
            "e.g. https://myaccountname.documents.azure.com:443/";
        private const string AUTH_KEY =
            "PRIMARY OR SECONDARY KEY OF THE ACCOUNT";
        private const string DATABASE =
            "NAME OF YOUR DATABASE";
        private const string COLLECTION =
            "NAME OF YOUR COLLECTION WITHIN THE DATABASE";

        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        private async static Task MainAsync()
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), AUTH_KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DATABASE, COLLECTION);

            await ListAllDocumentsAsync(client, collectionUri);
            //await ListOnlyVerticesAsync(client, collectionUri);
            //await AddTrivialVertexAsync(client, collectionUri);
            //await AddVertexWithPropertiesAsync(client, collectionUri);
            await AddEdgeAsync(client, collectionUri);
        }

        private async static Task ListAllDocumentsAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var query = client.CreateDocumentQuery(
                collectionUri,
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                });
            var queryAll = query.AsDocumentQuery();
            var all = await GetAllResultsAsync(queryAll);

            Console.WriteLine($"Collection contains {all.Length} documents:");

            foreach (var d in all)
            {
                var json = GetJson(d);

                if (d.Id == "CarolToAlice")
                {
                    await client.DeleteDocumentAsync(
                        d.SelfLink,
                        new RequestOptions
                        {
                            PartitionKey = new PartitionKey(d.GetPropertyValue<string>("department"))
                        });
                }

                Console.WriteLine(json);
            }

            Console.WriteLine();
        }

        private async static Task ListOnlyVerticesAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var query = client.CreateDocumentQuery<MinimalDoc>(
                collectionUri,
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                });
            var queryVertex = (from d in query
                               where !d._isEdge.HasValue
                               select d).AsDocumentQuery();
            var all = await GetAllResultsAsync(queryVertex);

            Console.WriteLine($"Collection contains {all.Length} documents:");

            foreach (var d in all)
            {
                Console.WriteLine(d.id);
            }

            Console.WriteLine();
        }

        private async static Task AddTrivialVertexAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var response = await client.CreateDocumentAsync(
                collectionUri,
                new
                {
                    id = "Carol",
                    label = "person",
                    department = "support character"
                });
            var json = GetJson(response.Resource);

            Console.WriteLine(json);
        }

        private async static Task AddVertexWithPropertiesAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var response = await client.CreateDocumentAsync(
                collectionUri,
                new
                {
                    id = "David",
                    label = "person",
                    age = new[] {
                        new
                        {
                            id = Guid.NewGuid().ToString(),
                            _value = 48
                        }
                    },
                    department = "support character"
                });
            var json = GetJson(response.Resource);

            Console.WriteLine(json);
        }

        private static async Task AddEdgeAsync(DocumentClient client, Uri collectionUri)
        {
            var response = await client.CreateDocumentAsync(
                collectionUri,
                new
                {
                    _isEdge = true,
                    id = "CarolToAlice",
                    label = "eavesdropOn",
                    language = "English",
                    department = "support character",
                    _vertexId = "Carol",
                    _vertexLabel = "person",
                    _sink = "Alice",
                    _sinkLabel = "person",
                    _sinkPartition = "stereotype"
                });
            var json = GetJson(response.Resource);

            Console.WriteLine(json);
        }

        private static string GetJson(object obj)
        {
            var serializer = new JsonSerializer();

            using (var writer = new StringWriter())
            {
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, obj);
                writer.Flush();

                return writer.ToString();
            }
        }

        private async static Task<T[]> GetAllResultsAsync<T>(IDocumentQuery<T> queryAll)
        {
            var list = new List<T>();

            while (queryAll.HasMoreResults)
            {
                var docs = await queryAll.ExecuteNextAsync<T>();

                foreach (var d in docs)
                {
                    list.Add(d);
                }
            }

            return list.ToArray();
        }

        private async static Task<int> ProcessAllResultsAsync<T>(
            IDocumentQuery<T> queryAll,
            Action<T> action)
        {
            int count = 0;

            while (queryAll.HasMoreResults)
            {
                var docs = await queryAll.ExecuteNextAsync<T>();

                foreach (var d in docs)
                {
                    action(d);
                    ++count;
                }
            }

            return count;
        }
    }
}