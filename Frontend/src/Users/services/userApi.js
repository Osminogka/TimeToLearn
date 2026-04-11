import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/user`;

const getByName = async (name) => {
    return httpClient.getAuth(`${BASE_URL}/${encodeURIComponent(name)}`);
};

const getAllUsers = async () => {
    return httpClient.getAuth(`${BASE_URL}/all`);
};

const updateProfile = async (payload) => {
    return httpClient.postAuth(`${BASE_URL}/update`, payload);
};

export default {
    getAllUsers,
    getByName,
    updateProfile,
};
