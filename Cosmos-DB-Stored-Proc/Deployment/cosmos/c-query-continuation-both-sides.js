//  Query with continuation on both sides:  do the query in a sproc and continue paging the results
//  ; the sproc returns continuation token so it can be called multiple times and get around the
//  5 seconds limit.
//
//  We implement a "SELECT * FROM c WHERE c.oneThird=1" by doing a
//  "SELECT * FROM c" and then doing the filtering in code
function countOnes(sprocContinuationToken) {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var oneCount = 0;

    if (sprocContinuationToken) {   //  Parse the token
        var token = JSON.parse(sprocContinuationToken);

        if (!token.countSoFar) {
            throw new Error('Bad token format:  no count');
        }
        if (!token.queryContinuationToken) {
            throw new Error('Bad token format:  no continuation');
        }
        //  Retrieve "count so far"
        oneCount = token.countSoFar;
        //  Retrieve query continuation token to continue paging
        query(token.queryContinuationToken);
    }
    else {  //  Start a recursion
        query();
    }

    //  Function within the main stored procedure function
    function query(queryContinuation) {
        var requestOptions = { continuation: queryContinuation };
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
                    response.setBody({ count: oneCount, continuation: null });
                }
            });

        if (!isAccepted) {
            var sprocToken = JSON.stringify({
                countSoFar: oneCount,
                queryContinuationToken: queryContinuation
            });

            response.setBody({ count: null, continuation: sprocToken });
        }
    }
}