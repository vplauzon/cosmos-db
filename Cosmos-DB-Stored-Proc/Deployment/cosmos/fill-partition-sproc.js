function sproc1() {
  var response = getContext().getResponse();

  response.setBody(JSON.stringify(42));
}