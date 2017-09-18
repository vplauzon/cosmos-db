function trigger() {
    var context = getContext();
    var request = context.getRequest();

    //  Document to be created in the current operation
    var documentToCreate = request.getBody();

    //  Validate the document is an edge:
    //  Does it have a label?
    if ('label' in documentToCreate) {
        documentToCreate.isEdge = true
    }

    //  Update the document that will be created
    request.setBody(documentToCreate);
}