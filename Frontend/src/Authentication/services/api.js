const BASE_URL = '/api/a/authentication';

import { saveToken } from '../../Shared/services/utils';

/**
 * Helper function to handle POST requests.
 * @param {string} endpoint - The API endpoint (relative to BASE_URL).
 * @param {object} data - The request body data to send.
 * @returns {Promise<any>} - Parsed JSON response or throws an error.
 */
const postRequest = async (endpoint, data) => {
    const url = `${BASE_URL}${endpoint}`;
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'API request failed');
    }

    return response.json();
};

// Authentication functions
const login = async (email, password) => {
    const result = await postRequest('/login', { email, password });
    if (result.success) {
        saveToken(result.message); // Save the JWT using utils.js
    }
    return result;
};

const register = async (name, email, password) => {
    const result = await postRequest('/register', { name, email, password });
    if (result.success) {
        saveToken(result.message); // Save the JWT using utils.js
    }
    return result;
};

const getRole = async (email) => {
    const result = await fetch(`/api/u/general/${email}`, {
        method: 'GET'
    });
    if (!result.ok) {
        const error = await result.json();
        return error;
    }
    else {
        return result.json();
    }
};

export default {
    login,
    register,
    getRole
};
