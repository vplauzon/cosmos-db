function createRecords(partitionKey, recordCount, geoRatio) {
    var context = getContext();
    var collection = context.getCollection();
    var recordCreated = 0;
    var geoThreshold = recordCount * geoRatio;
    var longitudeRange = 73.977105 - 73.459731;
    var latitudeRange = 45.708164 - 45.405527;

    for (i = 0; i < recordCount; i++) {
        var randomString = "";

        for (j = 0; j < 1024; ++j) {
            randomString += String.fromCharCode(Math.random() * 90 + 33);
        }

        var documentToCreate = {
            part: partitionKey,
            name: randomString.substr(0, 64),
            profile: {
                age: Math.round(Math.random() * 100),
                salary: Math.round(Math.random() * 10000000) / 100,
                project: randomString.substr(64, 128)
            },
            alias: {
                name: randomString.substr(192, 256),
                reference: randomString.substr(448, 256)
            },
            weapon: randomString.substr(704, 320)
        };

        if (i < geoThreshold) {
            documentToCreate.location = {
                type: "Point",
                coordinates: [
                    -73.977105 + Math.random() * longitudeRange,
                    45.405527 + Math.random() * latitudeRange]
            };
        }

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