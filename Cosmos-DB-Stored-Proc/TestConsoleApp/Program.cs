using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;

namespace TestConsoleApp
{
    class Program
    {
        private const string ENDPOINT =
            "https://cosmos-test-account-avboxnch6jlig.documents.azure.com:443/";
        private const string KEY =
            "AlBwjdslCY1FlmR3zOU5DEzeRYQmu447EzHYYWJk7bMGXz32PAyRVjLwho9qawtw746xNKdM4WVf2z4SzRmbKA==";
        private const string DB = "my-db";
        private const string COLLECTION = "my-collection";
        private const string DEFAULT_PARTITION_KEY = "ABC";
        private const int RECORD_COUNT = 10000;

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
            await FillPartitionAsync();
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

                Console.WriteLine($"{recordsCreated} records created");
                remainCount -= recordsCreated;
            }
            Console.WriteLine("Done invoking fillPartition");
        }
    }
}