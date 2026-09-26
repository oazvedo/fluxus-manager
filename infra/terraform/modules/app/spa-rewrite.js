// CloudFront Function (viewer-request) do frontend: rotas do React Router sem extensão
// (ex.: /empresas/123) servem o index.html; arquivos (ex.: /assets/app.js) passam direto.
function handler(event) {
  var request = event.request;
  if (request.uri.indexOf('.') === -1) {
    request.uri = '/index.html';
  }
  return request;
}
