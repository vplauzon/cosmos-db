using System;
using Microsoft.Azure.Documents.Client;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using System.Diagnostics;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        private const string SERVICE_ENDPOINT = "https://vplgeo.documents.azure.com:443/";
        private const string KEY = "XQxGE8fS5i4MompBN3AUtJg0Yd8lV0krXp9Q7gchVmw1C8u3AMkfgJpH3hsQr4TB7P4FWUmFrcqKeOeTvvQfOA==";
        private const string DB = "mydb";
        private const string COLLECTION = "mycoll";
        private const string SPROC = "createRecords";

        static void Main(string[] args)
        {
            SpatialTestAsync().Wait();
            //SprocTestAsync().Wait();
            //CleanAsync().Wait();
        }

        private async static Task SpatialTestAsync()
        {
            const int ITERATION_COUNT = 5;

            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DB, COLLECTION);
            var collection = (await client.ReadDocumentCollectionAsync(collectionUri)).Resource;
            var centerStart = Tuple.Create(-73.94, 45.51);
            var centerIncrement = Tuple.Create(.01, .01);

            Console.WriteLine("Radius, EdgeCount, AvgPointCount, Elaspsed");
            for (var radius = .005; radius < 1; radius += .05)
            {
                for (var edgeCount = 4; edgeCount < 14; edgeCount += 4)
                {
                    var watch = Stopwatch.StartNew();
                    long totalCount = 0;

                    for (var i = 0; i != ITERATION_COUNT; ++i)
                    {
                        var center = Tuple.Create(
                            centerStart.Item1 + i * centerIncrement.Item1,
                            centerStart.Item2 + i * centerIncrement.Item2);
                        var count = await SpacialIterationAsync(
                            client, collectionUri, center, radius, edgeCount);

                        totalCount += count;
                    }

                    Console.WriteLine($"{radius}, {edgeCount}, {(double)totalCount / ITERATION_COUNT}, {watch.Elapsed / ITERATION_COUNT}");
                }
            }
        }

        private static async Task<long> SpacialIterationAsync(
            DocumentClient client,
            Uri collectionUri,
            Tuple<double, double> center,
            double radius,
            int edgeCount)
        {
            var polyCoordinates = new[] { CreatePolygon(center, radius, edgeCount) };
            var parameters = new SqlParameterCollection(new[]
            {
                new SqlParameter("@polyCoordinates", polyCoordinates)
            });
            var query = client.CreateDocumentQuery<long>(
                collectionUri,
                new SqlQuerySpec(
                    "SELECT VALUE COUNT(1) "
                    + "FROM record r "
                    + "WHERE ST_WITHIN(r.location,"
                    + " {'type':'Polygon', 'coordinates':@polyCoordinates})",
                    parameters),
                new FeedOptions { EnableCrossPartitionQuery = true });
            var queryAll = query.AsDocumentQuery();

            while (queryAll.HasMoreResults)
            {
                var docs = await queryAll.ExecuteNextAsync();

                return docs.First();
            }

            throw new InvalidOperationException("Nothing returned from the service");
        }

        private static double[][] CreatePolygon(
            Tuple<double, double> center,
            double radius,
            int edgeCount)
        {
            if (edgeCount < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeCount));
            }
            if (radius <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            var edges = new double[edgeCount + 1][];

            for (int i = 1; i < edgeCount; ++i)
            {
                var angle = 2 * i * Math.PI / edgeCount;
                var longitude = center.Item1 + radius * Math.Cos(angle);
                var latitude = center.Item2 + radius * Math.Sin(angle);

                edges[i] = new[] { longitude, latitude };
            }

            edges[0] = edges[edgeCount] = new[] { center.Item1 + radius, center.Item2 };

            return edges;
        }

        private async static Task CleanAsync()
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DB, COLLECTION);
            var collection = (await client.ReadDocumentCollectionAsync(collectionUri)).Resource;
            var query = client.CreateDocumentQuery(
                collectionUri,
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                });
            var queryAll = query.AsDocumentQuery<Document>();

            while (queryAll.HasMoreResults)
            {
                var docs = await queryAll.ExecuteNextAsync<Document>();

                foreach (var doc in docs)
                {
                    await client.DeleteDocumentAsync(doc.SelfLink, new RequestOptions
                    {
                        PartitionKey = new PartitionKey(doc.GetPropertyValue<string>("part"))
                    });
                    //Console.WriteLine();
                }
            }
        }

        private async static Task SprocTestAsync()
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), KEY);
            var sprocUri = UriFactory.CreateStoredProcedureUri(DB, COLLECTION, SPROC);
            var response = await client.ExecuteStoredProcedureAsync<int>(
                sprocUri,
                new RequestOptions
                {
                    PartitionKey = new PartitionKey("vince")
                },
                "vince",
                3,
                .3);

            Console.WriteLine(response.Response);
        }
    }
}