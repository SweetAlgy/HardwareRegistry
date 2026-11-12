import { HttpErrorResponse } from '@angular/common/http';
import { toProblemDetails } from './problem-details.model';

describe('toProblemDetails', () => {
  it('toProblemDetails_httpErrorWithProblemBody_returnsBody', () => {
    const body = { status: 409, title: 'Conflict', code: 'Device.AlreadyExists' };

    const problem = toProblemDetails(new HttpErrorResponse({ status: 409, error: body }));

    expect(problem).toEqual(body);
  });

  it('toProblemDetails_httpErrorWithTextBody_returnsNull', () => {
    expect(
      toProblemDetails(new HttpErrorResponse({ status: 502, error: 'Bad gateway' })),
    ).toBeNull();
  });

  it('toProblemDetails_httpErrorWithoutBody_returnsNull', () => {
    expect(toProblemDetails(new HttpErrorResponse({ status: 0, error: null }))).toBeNull();
  });

  it('toProblemDetails_nonHttpError_returnsNull', () => {
    expect(toProblemDetails(new Error('boom'))).toBeNull();
    expect(toProblemDetails({ status: 409 })).toBeNull();
  });
});
