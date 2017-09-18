function trigger(){
    var context = getContext();
    var request = context.getRequest();
    
    //  Document to be created in the current operation
    var documentToCreate = request.getBody();

    //  Validate the document is an edge:
    //  Does it have a label?
    if('label' in documentToCreate){
        var properties = [
            "name",
            "age",
            "country"
        ];

        for(let p of properties){
            var obj = new Object;

            obj._value = documentToCreate[p];
            obj.id = "1";

            documentToCreate[p] = [obj];
        }
    }

    //  Update the document that will be created
    request.setBody(documentToCreate);
}