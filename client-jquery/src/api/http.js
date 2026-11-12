import $ from 'jquery';

export function request(method, url, body) {
  return $.ajax({
    url,
    method,
    contentType: 'application/json',
    data: body === undefined ? undefined : JSON.stringify(body),
  });
}

/** Error body the API returns for every failed request, or `null` when there is none. */
export function toProblemDetails(jqXhr) {
  const body = jqXhr?.responseJSON;

  return body !== null && typeof body === 'object' && 'status' in body ? body : null;
}
