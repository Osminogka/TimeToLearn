import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';
import { saveToken } from '@/Shared/services/utils';

const BASE_URL = SERVICE_BASE_PATHS.auth;

const login = async (email, password) => {
    const result = await httpClient.post(`${BASE_URL}/login`, { email, password });
    if (result.success) {
        saveToken(result.message);
    }
    return result;
};

const register = async (name, email, password) => {
    const result = await httpClient.post(`${BASE_URL}/register`, { name, email, password });
    if (result.success) {
        saveToken(result.message);
    }
    return result;
};

export default {
    login,
    register,
};
