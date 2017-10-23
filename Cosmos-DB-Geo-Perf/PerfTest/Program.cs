using System;
using Microsoft.Azure.Documents.Client;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Program
    {
        private delegate SqlQuerySpec CreateQuerySpec(
            double radius,
            int edgeCount,
            int iterationIndex);

        private const string SERVICE_ENDPOINT = "https://vplgeo.documents.azure.com:443/";
        private const string KEY = "XQxGE8fS5i4MompBN3AUtJg0Yd8lV0krXp9Q7gchVmw1C8u3AMkfgJpH3hsQr4TB7P4FWUmFrcqKeOeTvvQfOA==";
        private const string DB = "mydb";
        private const string COLLECTION = "mycoll";
        private const string SPROC = "createRecords";

        static void Main(string[] args)
        {
            TestAsync().Wait();
        }

        private async static Task TestAsync()
        {
            //await WithinTestAsync();
            await WithinClosestTestAsync();

            //await SprocTestAsync();
            //await CleanAsync();
        }

        private async static Task WithinTestAsync()
        {
            var centerStart = Tuple.Create(-73.94, 45.51);
            var centerIncrement = Tuple.Create(.01, .01);

            await RunPerformanceAsync(
                5,
                new[] { .005, .05, .1 },
                new[] { 4, 10, 25, 50 },
                (radius, edgeCount, iterationIndex) =>
                {
                    var center = Tuple.Create(
                        centerStart.Item1 + iterationIndex * centerIncrement.Item1,
                        centerStart.Item2 + iterationIndex * centerIncrement.Item2);
                    var polyCoordinates = new[] { CreatePolygon(center, radius, edgeCount) };
                    var parameters = new SqlParameterCollection(new[]
                    {
                        new SqlParameter("@polyCoordinates", polyCoordinates)
                    });
                    var querySpec = new SqlQuerySpec(
                        "SELECT VALUE COUNT(1) "
                        + "FROM record r "
                        + "WHERE ST_WITHIN(r.location,"
                        + " {'type':'Polygon', 'coordinates':@polyCoordinates})",
                        parameters);

                    return querySpec;
                });
        }

        private async static Task WithinClosestTestAsync()
        {
            var centerStart = Tuple.Create(-73.94, 45.51);
            var centerIncrement = Tuple.Create(.01, .01);

            await RunPerformanceAsync(
                5,
                new[] { .005, .05, .1 },
                new[] { 4, 10, 25, 50 },
                (radius, edgeCount, iterationIndex) =>
                {
                    var center = Tuple.Create(
                        centerStart.Item1 + iterationIndex * centerIncrement.Item1,
                        centerStart.Item2 + iterationIndex * centerIncrement.Item2);
                    var polyCoordinates = new[] { CreatePolygon(center, radius, edgeCount) };
                    var parameters = new SqlParameterCollection(new[]
                    {
                        new SqlParameter("@polyCoordinates", polyCoordinates)
                    });
                    var querySpec = new SqlQuerySpec(
                        "SELECT VALUE COUNT(1) "
                        + "FROM record r "
                        + "WHERE ST_WITHIN(r.location,"
                        + " {'type':'Polygon', 'coordinates':@polyCoordinates})",
                        parameters);

                    return querySpec;
                });
        }

        private static async Task RunPerformanceAsync(
            int iterationCount,
            IEnumerable<double> radii,
            IEnumerable<int> edgeCounts,
            CreateQuerySpec createQuerySpec)
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DB, COLLECTION);
            var collection = (await client.ReadDocumentCollectionAsync(collectionUri)).Resource;

            Console.WriteLine("Radius, EdgeCount, IterationCount, AvgMeasure, Elaspsed");
            foreach (var radius in radii)
            {
                foreach (var edgeCount in edgeCounts)
                {
                    var watch = Stopwatch.StartNew();
                    long totalMeasure = 0;

                    for (var i = 0; i != iterationCount; ++i)
                    {
                        var spec = createQuerySpec(radius, edgeCount, i);
                        var query = client.CreateDocumentQuery<long>(
                            collectionUri,
                            spec,
                            new FeedOptions { EnableCrossPartitionQuery = true });
                        var measure =
                            await QueryMeasureAsync(client, collectionUri, query);

                        totalMeasure += measure;
                    }

                    Console.WriteLine($"{radius}, {edgeCount}, {iterationCount}, "
                        + $"{(double)totalMeasure / iterationCount}, "
                        + $"{watch.Elapsed / iterationCount}");
                }
            }
        }

        private static async Task<long> QueryMeasureAsync(
            DocumentClient client, Uri collectionUri, IQueryable<long> query)
        {
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