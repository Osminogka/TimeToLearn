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
        const errorMessage = typeof payload === 'object' && payload !== null
            ? (payload.message || 'API request failed')
            : 'API request failed';
        throw new Error(errorMessage);
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
