import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/user`;

const getByName = async (name) => {
    return httpClient.get(`${BASE_URL}/${encodeURIComponent(name)}`);
};

const updateProfile = async (payload) => {
    return httpClient.post(`${BASE_URL}/update`, payload);
};

export default {
    getByName,
    updateProfile,
};
