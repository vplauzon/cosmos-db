//  Query with continuation:  do the query in a sproc and continue paging the results
//
//  We implement a "SELECT * FROM c WHERE c.oneThird=1" by doing a
//  "SELECT * FROM c" and then doing the filtering in code
//
//  Problem:  Although this sproc implements continuation on the server side and scale
//  better, it won't scale to tens of thousands of records.  Cosmos DB imposes a 5 seconds
//  limit on any query which will force the sproc to stop.  When it does it will throw the
//  the exception at the end of the sproc.
function countOnes() {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var oneCount = 0;

    //  Start a recursion
    query();

    //  Function within the main stored procedure function
    function query(continuation) {
        var requestOptions = { continuation: continuation };
        //  Query all documents
        var isAccepted = collection.queryDocuments(
            collection.getSelfLink(),
            "SELECT * FROM c",
            requestOptions,
            function (err, feed, responseOptions) {
                if (err) {
                    throw err;
                }

                //  Scan results
                if (feed) {
                    for (var i = 0; i != feed.length; ++i) {
                        var doc = feed[i];

                        //  Filter document with 'oneThird' == 1
                        if (doc.oneThird == 1) {
                            ++oneCount;
                        }
                    }
                }

                if (responseOptions.continuation) {
                    //  Continue the query
                    query(responseOptions.continuation)
                } else {
                    //  Return the count in the response
                    response.setBody(oneCount);
                }
            });

        if (!isAccepted) {
            throw new Error('The query was not accepted by the server.');
        }
    }
}