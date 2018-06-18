function createRecords(partitionKey, recordCount) {
    var context = getContext();
    var collection = context.getCollection();
    var randomString = "";

    for (j = 0; j < 256; ++j) {
        randomString += String.fromCharCode(Math.random() * 90 + 33);
    }

    createRecord(0, randomString);

    function createRecord(recordCreated, randomString) {
        var documentToCreate = {
            part: partitionKey,
            recordNumber: recordCreated,
            oneThird: recordCreated % 3,
            name: randomString.substr(0, 64),
            profile: {
                age: Math.round(Math.random() * 100),
                salary: Math.round(Math.random() * 10000000) / 100,
                project: randomString.substr(64, 64)
            },
            alias: {
                name: randomString.substr(128, 64),
                reference: randomString.substr(192, 64)
            }
        };

        var accepted = collection.createDocument(
            collection.getSelfLink(),
            documentToCreate,
            function (err, documentCreated) {
                if (err) {
                    throw new Error('Error' + err.message);
                }
                else if (recordCreated < recordCount) {
                    createRecord(
                        recordCreated + 1,
                        //  Shuffle the random string
                        randomString.substr(100, 156) + randomString.substr(0, 100));
                }
                else {
                    context.getResponse().setBody(recordCreated);
                }
            });

        if (!accepted) {
            context.getResponse().setBody(recordCreated);
        }
    }
}