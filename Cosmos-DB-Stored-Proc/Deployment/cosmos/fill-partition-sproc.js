function createRecords(partitionKey, recordCount) {
    var context = getContext();
    var collection = context.getCollection();
    var recordCreated = 0;

    for (i = 0; i < recordCount; i++) {
        var randomString = "";

        for (j = 0; j < 1024; ++j) {
            randomString += String.fromCharCode(Math.random() * 90 + 33);
        }

        var documentToCreate = {
            part: partitionKey,
            oneThird: Math.round(Math.random() * 3),
            name: randomString.substr(0, 64),
            profile: {
                age: Math.round(Math.random() * 100),
                salary: Math.round(Math.random() * 10000000) / 100,
                project: randomString.substr(64, 128)
            },
            alias: {
                name: randomString.substr(64, 256),
                reference: randomString.substr(300, 256)
            }
        };

        var accepted = collection.createDocument(
            collection.getSelfLink(),
            documentToCreate,
            function (err, documentCreated) {
                if (err) {
                    throw new Error('Error' + err.message);
                }
                else {
                    ++recordCreated;
                }
            });

        if (!accepted)
            return;
    }

    context.getResponse().setBody(recordCreated);
}