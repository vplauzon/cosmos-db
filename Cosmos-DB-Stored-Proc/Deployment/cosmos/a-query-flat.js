function countOnes() {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var oneCount = 0;

    //  Query all documents
    var isAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        "SELECT * FROM c",
        {},
        function (err, feed, responseOptions) {
            if (err) {
                throw err;
            }

            if (feed) {
                for (var i = 0; i != feed.length; ++i) {
                    var doc = feed[i];

                    //  Filter document with 'oneThird' == 1
                    if (doc.oneThird == 1) {
                        ++oneCount;
                    }
                }
            }

            //  Return the count in the response
            response.setBody(oneCount);
        });

    if (!isAccepted) {
        throw new Error('The query was not accepted by the server.');
    }
}