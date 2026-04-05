class ApiError extends Error {
    constructor(message, status, details = null) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.details = details;
    }
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

async function request(url, options = {}) {
    const response = await fetch(url, {
        headers: {
            'Content-Type': 'application/json',
            ...(options.headers || {}),
        },
        ...options,
    });

    const isJson = response.headers.get('content-type')?.includes('application/json');
    const payload = isJson ? await response.json() : await response.text();

    if (!response.ok) {
        const errorMessage = extractErrorMessage(payload);
        throw new ApiError(errorMessage, response.status, payload);
    }

    return payload;
}

export const httpClient = {
    get: (url, options = {}) => request(url, { ...options, method: 'GET' }),
    post: (url, data, options = {}) => request(url, {
        ...options,
        method: 'POST',
        body: JSON.stringify(data),
    }),
};

export { ApiError };
