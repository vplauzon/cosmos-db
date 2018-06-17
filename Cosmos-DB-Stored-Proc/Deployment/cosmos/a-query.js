function countOnes() {
    var response = getContext().getResponse();
    var collection = getContext().getCollection();
    var oneCount = 0;

    // Query documents for all captions
    var isAccepted = collection.queryDocuments(
        collection.getSelfLink(),
        "SELECT * FROM c",
        null,
        function (err, feed, responseOptions) {
            if (err) {
                throw err;
            }

            if (feed) {
                for (var i = 0; i != feed.length; ++i) {
                    var doc = feed[i];

                    if (doc.oneThird == 1) {
                        ++oneCount;
                    }
                }
            }

            response.setBody(countDict);
        });

    if (!isAccepted) {
        throw new Error('The query was not accepted by the server.');
    }
}