using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;

namespace TestConsoleApp
{
    class Program
    {
        private const string ENDPOINT =
            "https://cosmos-test-account-genpmujkde7iu.documents.azure.com:443/";
        private const string KEY =
            "hxXWccELPp8eYavWGqlzRopCB0d5d4jTXFA3gGs58PEstFZmCkq4dF24Jjuv8zxC3iKKGCxwXcZ401uhyt8lGg==";
        private const string DB = "my-db";
        private const string COLLECTION = "my-collection";
        private const string DEFAULT_PARTITION_KEY = "ABC";
        private const int RECORD_COUNT = 25000;

        private static readonly DocumentClient _client =
            new DocumentClient(new Uri(ENDPOINT), KEY);
        private static readonly Uri _collectionUri =
            UriFactory.CreateDocumentCollectionUri(DB, COLLECTION);
        private static readonly RequestOptions _defaultRequestOptions = new RequestOptions
        {
            PartitionKey = new PartitionKey(DEFAULT_PARTITION_KEY)
        };

        static void Main(string[] args)
        {
            MainAsync().Wait();
        }


        private static async Task MainAsync()
        {
            //await FillPartitionAsync();
            //await QueryAsync();
        }

        private static async Task QueryAsync()
        {
            string continuation = null;

            Console.WriteLine("Invoke query-c");
            do
            {
                var response = await _client.ExecuteStoredProcedureAsync<QueryOutput>(
                    UriFactory.CreateStoredProcedureUri(DB, COLLECTION, "c-query-continuation-both-sides"),
                    _defaultRequestOptions,
                    continuation);
                var output = response.Response;

                if (output.Count.HasValue)
                {
                    Console.WriteLine($"Count:  {output.Count}");
                    continuation = null;
                }
                else
                {
                    continuation = output.Continuation;
                    Console.WriteLine($"Continuation:  {continuation}");
                }
            }
            while (continuation != null);
            Console.WriteLine("Done invoking query-c");
        }

        private static async Task FillPartitionAsync()
        {
            var remainCount = RECORD_COUNT;

            Console.WriteLine("Invoke fillPartition");
            while (remainCount > 0)
            {
                var response = await _client.ExecuteStoredProcedureAsync<int>(
                    UriFactory.CreateStoredProcedureUri(DB, COLLECTION, "fillPartition"),
                    _defaultRequestOptions,
                    DEFAULT_PARTITION_KEY,
                    remainCount);
                var recordsCreated = response.Response;

                remainCount -= recordsCreated;
                Console.WriteLine($"{recordsCreated} records created, {remainCount} remains");
            }
            Console.WriteLine("Done invoking fillPartition");
        }
    }
}