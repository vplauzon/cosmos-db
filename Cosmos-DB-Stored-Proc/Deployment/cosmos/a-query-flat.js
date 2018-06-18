//  Flat query:  simply do the query in a sproc
//
//  We implement a "SELECT * FROM c WHERE c.oneThird=1" by doing a
//  "SELECT * FROM c" and then doing the filtering in code
//
//  Problem:  Although this sproc is simple, it doesn't scale.
//  It only select a page of result and hence won't return a good result
//  if the partition has a few thousand records.
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