function countOnes() {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var oneCount = 0;

    //  Start a recursion
    query();

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