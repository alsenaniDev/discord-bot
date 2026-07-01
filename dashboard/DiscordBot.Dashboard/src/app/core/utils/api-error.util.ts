import { HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ApiErrorBody {
  message?: string;
  errors?: string[];
  detail?: string;
  title?: string;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = error.error as ApiErrorBody | string | null;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    if (body.errors?.length) {
      return body.errors.join(' ');
    }
    if (body.message) {
      return body.message;
    }
    if (body.detail) {
      return body.detail;
    }
    if (body.title) {
      return body.title;
    }
  }

  if (error.status === 0) {
    return `Cannot reach the API at ${environment.apiUrl}. Check that the API is online and CORS allows this dashboard URL.`;
  }

  if (error.status === 401) {
    return 'Your session expired. Please log in again.';
  }

  if (error.status === 404) {
    return 'The requested resource was not found.';
  }

  return fallback;
}
