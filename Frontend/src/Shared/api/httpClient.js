import { getToken } from '@/Shared/services/utils';

class ApiError extends Error {
    constructor(message, status, details = null) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.details = details;
    }
}

const HTML_ERROR_PATTERN = /<!doctype|<html|<head|<body/i;

function getDefaultMessageByStatus(status) {
    switch (status) {
        case 400:
            return 'The request was invalid. Please check your input and try again.';
        case 401:
            return 'Your session is not authorized. Please sign in again.';
        case 403:
            return 'You do not have permission to perform this action.';
        case 404:
            return 'The requested resource was not found.';
        case 409:
            return 'This action conflicts with existing data.';
        case 422:
            return 'Some input values are invalid. Please review and try again.';
        case 429:
            return 'Too many requests. Please wait a moment and retry.';
        case 500:
            return 'Server error occurred. Please try again later.';
        case 502:
            return 'Gateway error (502): service is unavailable or restarting. Please try again shortly.';
        case 503:
            return 'Service is temporarily unavailable. Please retry in a moment.';
        case 504:
            return 'Request timed out while waiting for the service. Please retry.';
        default:
            return 'API request failed';
    }
}

function isHtmlPayload(payload) {
    return typeof payload === 'string' && HTML_ERROR_PATTERN.test(payload);
}

function extractErrorMessage(payload) {
    if (typeof payload === 'string') {
        const trimmed = payload.trim();
        return trimmed.length > 0 ? trimmed : 'API request failed';
    }

    if (payload && typeof payload === 'object') {
        if (typeof payload.message === 'string' && payload.message.trim().length > 0) {
            return payload.message;
        }

        if (typeof payload.title === 'string' && payload.title.trim().length > 0) {
            return payload.title;
        }

        if (payload.errors && typeof payload.errors === 'object') {
            const firstKey = Object.keys(payload.errors)[0];
            const firstValue = firstKey ? payload.errors[firstKey] : null;
            if (Array.isArray(firstValue) && firstValue.length > 0) {
                return firstValue[0];
            }
        }
    }

    return 'API request failed';
}

function buildFriendlyErrorMessage(status, payload) {
    if (isHtmlPayload(payload)) {
        return getDefaultMessageByStatus(status);
    }

    const extractedMessage = extractErrorMessage(payload);
    if (!extractedMessage || extractedMessage === 'API request failed') {
        return getDefaultMessageByStatus(status);
    }

    // Avoid leaking huge server pages or stack traces directly to users.
    if (extractedMessage.length > 240) {
        return getDefaultMessageByStatus(status);
    }

    return extractedMessage;
}

async function request(url, options = {}) {
    const {
        requiresAuth = false,
        headers: customHeaders = {},
        ...fetchOptions
    } = options;

    const headers = {
        'Content-Type': 'application/json',
        ...customHeaders,
    };

    if (requiresAuth) {
        const token = getToken();
        if (!token) {
            throw new ApiError('Authorization required. Please sign in and try again.', 401);
        }
        headers.Authorization = `Bearer ${token}`;
    }

    let response;

    try {
        response = await fetch(url, {
            ...fetchOptions,
            headers,
        });
    } catch (error) {
        throw new ApiError('Network error: unable to reach API. Check connection, proxy, or ingress.', 0, error);
    }

    const isJson = response.headers.get('content-type')?.includes('application/json');
    const payload = isJson ? await response.json() : await response.text();

    if (!response.ok) {
        const errorMessage = buildFriendlyErrorMessage(response.status, payload);
        throw new ApiError(errorMessage, response.status, payload);
    }

    return payload;
}

export const httpClient = {
    get: (url, options = {}) => request(url, { ...options, method: 'GET' }),
    getAuth: (url, options = {}) => request(url, { ...options, method: 'GET', requiresAuth: true }),
    post: (url, data, options = {}) => request(url, {
        ...options,
        method: 'POST',
        body: JSON.stringify(data),
    }),
    postAuth: (url, data, options = {}) => request(url, {
        ...options,
        method: 'POST',
        requiresAuth: true,
        body: JSON.stringify(data),
    }),
};

export { ApiError };
