import { ref } from 'vue';

export const user = ref({
  name: '',
  email: '',
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

export function getCurrentUser() {
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) {
      return null;
  }

  // Split the JWT string into three parts: header, payload, signature
  const parts = token.split('.');
  if (parts.length !== 3) {
      return null;
  }

  // Decode the payload
  const decoded = atob(parts[1]);
  const payload = JSON.parse(decoded);

  console.log(payload);

  user.value.name = payload.unique_name;
  user.value.email = payload.email;
  return payload;
}

/**
 * Remove the JWT token from localStorage.
 */
export const clearToken = () => {
  localStorage.removeItem(TOKEN_KEY);
};

export default {
  saveToken,
  getToken,
  isAuthenticated,
  clearToken,
  getCurrentUser,
};
