import { HttpErrorResponse } from '@angular/common/http';
import { toApiError } from './api-error';

describe('toApiError', () => {
  it('uses the detail and code of problem details', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        title: 'Conflict',
        detail: 'Class 2B exists.',
        code: 'class-name-taken',
      },
    });

    expect(toApiError(error)).toEqual({
      status: 409,
      code: 'class-name-taken',
      message: 'Class 2B exists.',
    });
  });

  it('lists the field errors of validation problem details', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        status: 400,
        title: 'One or more validation errors occurred.',
        code: 'validation-failed',
        errors: { Name: ['The Name field is required.'] },
      },
    });

    expect(toApiError(error).message).toContain('The Name field is required.');
  });

  it('reports network errors', () => {
    const error = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });

    expect(toApiError(error).message).toContain('not reachable');
  });

  it('reports server errors without details', () => {
    const error = new HttpErrorResponse({ status: 502, error: '<html>Bad gateway</html>' });

    expect(toApiError(error).message).toContain('unexpected error (502)');
  });

  it('unwraps errors of resources', () => {
    const httpError = new HttpErrorResponse({ status: 0 });

    expect(toApiError(new Error('wrapped', { cause: httpError })).status).toBe(0);
  });
});
