﻿using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Collections.Generic;
using System.Linq;

namespace DemoAsyncQuery
{
    class Program
    {
        #region Inner Types
        private class MinimalDoc
        {
            public string id { get; set; }
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
            TestAsync().Wait();
        }

        private async static Task TestAsync()
        {
            var client = new DocumentClient(new Uri(SERVICE_ENDPOINT), AUTH_KEY);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(DATABASE, COLLECTION);

            await TestAllGenericDocumentsAsync(client, collectionUri);
            await TestAllGenericDocumentsPrintAsYouGoAsync(client, collectionUri);
            await TestFilterGenericDocumentsAsync(client, collectionUri);
            await TestFilterMinimalDocumentsAsync(client, collectionUri);
        }

        private async static Task TestAllGenericDocumentsAsync(
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

            Console.WriteLine($"Collection contains {all.Length} documents");
            Console.WriteLine("Here are the IDs:");

            foreach (var d in all)
            {
                Console.WriteLine(d.Id);
            }

            Console.WriteLine();
        }

        private async static Task TestAllGenericDocumentsPrintAsYouGoAsync(
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

            Console.WriteLine("Collection IDs printed out just-in-time (JIT):");

            var count = await ProcessAllResultsAsync(queryAll, (d) => Console.WriteLine(d.Id));

            Console.WriteLine($"There was {count} items in the collection");

            Console.WriteLine();
        }

        private async static Task TestFilterGenericDocumentsAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var query = client.CreateDocumentQuery(
                collectionUri,
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                });
            var queryNoDog = (from d in query
                              where d.Id != "Dog"
                              select d).AsDocumentQuery();
            var all = await GetAllResultsAsync(queryNoDog);

            Console.WriteLine($"Query result contains {all.Length} documents");
            Console.WriteLine("Here are the IDs:");

            foreach (var d in all)
            {
                Console.WriteLine(d.Id);
            }

            Console.WriteLine();
        }

        private async static Task TestFilterMinimalDocumentsAsync(
            DocumentClient client,
            Uri collectionUri)
        {
            var query = client.CreateDocumentQuery<MinimalDoc>(
                collectionUri,
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                });
            var queryNoDog = (from d in query
                              where d.id != "Dog"
                              select d).AsDocumentQuery();
            var all = await GetAllResultsAsync(queryNoDog);

            Console.WriteLine($"Query result contains {all.Length} documents");
            Console.WriteLine("Here are the IDs (mapped to custom type):");

            foreach (var d in all)
            {
                Console.WriteLine(d.id);
            }

            Console.WriteLine();
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