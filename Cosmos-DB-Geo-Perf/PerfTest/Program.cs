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
        private const int ITERATION_COUNT = 5;
        private delegate SqlQuerySpec CreateQuerySpec(
            double radius,
            int edgeCount,
            int iterationIndex);

        private const string SERVICE_ENDPOINT = "https://<YOUR COSMOS DB ACCOUNT NAME>.documents.azure.com:443/";
        private const string KEY = "<YOUR KEY>";
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
            await FilteredWithinTestAsync();
            //await ClosestTestAsync();

            //await SprocTestAsync();
            //await CleanAsync();
        }

        private async static Task WithinTestAsync()
        {
            var centerStart = Tuple.Create(-73.94, 45.51);
            var centerIncrement = Tuple.Create(.01, .01);

            Console.WriteLine("Radius, EdgeCount, # points, Elapsed");
            foreach (var radius in new[] { .005, .05, .1 })
            {
                foreach (var edgeCount in new[] { 4, 10, 25, 50 })
                {
                    var watch = Stopwatch.StartNew();
                    long totalMeasure = 0;

                    for (var i = 0; i != ITERATION_COUNT; ++i)
                    {
                        var center = Tuple.Create(
                            centerStart.Item1 + i * centerIncrement.Item1,
                            centerStart.Item2 + i * centerIncrement.Item2);
                        var polyCoordinates = CreatePolygon(center, radius, edgeCount);
                        var measure = await QueryCollectionAsync<long>(
                            "SELECT VALUE COUNT(1) "
                            + "FROM record r "
                            + "WHERE ST_WITHIN(r.location,"
                            + " {'type':'Polygon', 'coordinates':[@polyCoordinates]})",
                            new SqlParameter("@polyCoordinates", polyCoordinates));

                        totalMeasure += measure;
                    }
                    Console.WriteLine($"{radius}, {edgeCount}, "
                        + $"{(double)totalMeasure / ITERATION_COUNT}, "
                        + $"{watch.Elapsed / ITERATION_COUNT}");
                }
            }
        }

        private async static Task FilteredWithinTestAsync()
        {
            var centerStart = Tuple.Create(-73.94, 45.51);
            var centerIncrement = Tuple.Create(.01, .01);

            Console.WriteLine("Radius, EdgeCount, # points, Elaspsed");
            foreach (var radius in new[] { .005, .05, .1 })
            {
                foreach (var edgeCount in new[] { 4, 10, 25, 50 })
                {
                    var watch = Stopwatch.StartNew();
                    long totalMeasure = 0;

                    for (var i = 0; i != ITERATION_COUNT; ++i)
                    {
                        var center = Tuple.Create(
                            centerStart.Item1 + i * centerIncrement.Item1,
                            centerStart.Item2 + i * centerIncrement.Item2);
                        var polyCoordinates = CreatePolygon(center, radius, edgeCount);
                        var measure = await QueryCollectionAsync<long>(
                            "SELECT VALUE COUNT(1) "
                            + "FROM record r "
                            + "WHERE ST_WITHIN(r.location,"
                            + " {'type':'Polygon', 'coordinates':[@polyCoordinates]}) "
                            + " AND r.profile.age<25",
                            new SqlParameter("@polyCoordinates", polyCoordinates));

                        totalMeasure += measure;
                    }
                    Console.WriteLine($"{radius}, {edgeCount}, "
                        + $"{(double)totalMeasure / ITERATION_COUNT}, "
                        + $"{watch.Elapsed / ITERATION_COUNT}");
                }
            }
        }

        private async static Task ClosestTestAsync()
        {
            var centerStart = Tuple.Create(-73.94433964264864, 45.51350017859535);
            var centerIncrement = Tuple.Create(.01, .01);

            Console.WriteLine("Radius, # points, Elapsed");
            foreach (var radius in new[] { 100, 1000, 3000, 10000 })
            {
                var watch = Stopwatch.StartNew();
                long totalMeasure = 0;

                for (var i = 0; i != ITERATION_COUNT; ++i)
                {
                    var center = Tuple.Create(
                        centerStart.Item1 + i * centerIncrement.Item1,
                        centerStart.Item2 + i * centerIncrement.Item2);
                    var measure = await QueryCollectionAsync<long>(
                        "SELECT "
                        + "VALUE COUNT(1) "
                        + "FROM record r "
                        + "WHERE ST_DISTANCE (r.location, {'type':'Point', 'coordinates':@center})<@radius",
                        new SqlParameter("@center", CreatePoint(center)),
                        new SqlParameter("@radius", radius));

                    totalMeasure += measure;
                }
                Console.WriteLine($"{radius}, {(double)totalMeasure / ITERATION_COUNT}, "
                    + $"{watch.Elapsed / ITERATION_COUNT}");
            }
        }

        private static async Task<T> QueryCollectionAsync<T>(
            string queryText,
            params SqlParameter[] sqlParameter)
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DB, COLLECTION);
            var collection = (await client.ReadDocumentCollectionAsync(collectionUri)).Resource;
            var parameters = new SqlParameterCollection(sqlParameter);
            var querySpec = new SqlQuerySpec(queryText, parameters);
            var query = client.CreateDocumentQuery<T>(
                collectionUri,
                querySpec,
                new FeedOptions { EnableCrossPartitionQuery = true });
            var measure = await QueryMeasureAsync(client, collectionUri, query);

            return measure;
        }

        private static async Task<T> QueryMeasureAsync<T>(
            DocumentClient client, Uri collectionUri, IQueryable<T> query)
        {
            var queryAll = query.AsDocumentQuery();

            while (queryAll.HasMoreResults)
            {
                var docs = await queryAll.ExecuteNextAsync();

                return docs.First();
            }

            throw new InvalidOperationException("Nothing returned from the service");
        }

        private static double[] CreatePoint(Tuple<double, double> center)
        {
            return new[] { center.Item1, center.Item2 };
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