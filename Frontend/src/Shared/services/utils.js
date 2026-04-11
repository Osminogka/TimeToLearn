import { ref } from 'vue';

export const user = ref({
    name: '',
    email: '',
    role: '',
});

const TOKEN_KEY = 'jwt';

/**
 * Save the JWT token to localStorage.
 * @param {string} token - The JWT token to save.
 */
export const saveToken = (token) => {
    localStorage.setItem(TOKEN_KEY, token);
};

/**
 * Retrieve the JWT token from localStorage.
 * @returns {string|null} - The JWT token, or null if not found.
 */
export const getToken = () => {
    return localStorage.getItem(TOKEN_KEY);
};

/**
 * Check if a JWT token exists, indicating authentication.
 * @returns {boolean} - True if the user is authenticated, false otherwise.
 */
export const isAuthenticated = () => {
    getCurrentUser();
    return user.value.name !== '';
};

function decodeBase64Url(value) {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
    return atob(padded);
}

function normalizeRoleValue(value) {
    if (!value) {
        return '';
    }

    if (Array.isArray(value)) {
        return String(value[0] || '').trim();
    }

    return String(value).trim();
}

function extractRoleClaim(payload) {
    return normalizeRoleValue(
        payload.role
        || payload.Role
        || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role']
    );
}

function toRoleKey(role) {
    return String(role || '').trim().toLowerCase();
}

export function isTeacherRole(role) {
    return toRoleKey(role) === 'teacher';
}

export function isStudentRole(role) {
    return toRoleKey(role) === 'student';
}

export function getCurrentRole() {
    getCurrentUser();
    return user.value.role;
}

export function hasRole(role) {
    return toRoleKey(getCurrentRole()) === toRoleKey(role);
}

export function getCurrentUser() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) {
        user.value.name = '';
        user.value.email = '';
        user.value.role = '';
        return null;
    }

    // Split the JWT string into three parts: header, payload, signature
    const parts = token.split('.');
    if (parts.length !== 3) {
        user.value.name = '';
        user.value.email = '';
        user.value.role = '';
        return null;
    }

    try {
        const decoded = decodeBase64Url(parts[1]);
        const payload = JSON.parse(decoded);

        user.value.name = payload.unique_name || '';
        user.value.email = payload.email || '';
        user.value.role = extractRoleClaim(payload);
        return payload;
    } catch {
        user.value.name = '';
        user.value.email = '';
        user.value.role = '';
        return null;
    }
}

/**
 * Remove the JWT token from localStorage.
 */
export const clearToken = () => {
    localStorage.removeItem(TOKEN_KEY);
    user.value.name = '';
    user.value.email = '';
    user.value.role = '';
};

export default {
    saveToken,
    getToken,
    isAuthenticated,
    clearToken,
    getCurrentUser,
    getCurrentRole,
    hasRole,
    isTeacherRole,
    isStudentRole,
};
