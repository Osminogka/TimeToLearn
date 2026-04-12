import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/user`;
const STUDENT_BASE_URL = `${SERVICE_BASE_PATHS.users}/student`;

const getByName = async (name) => {
    return httpClient.getAuth(`${BASE_URL}/${encodeURIComponent(name)}`);
};

const getAllUsers = async () => {
    return httpClient.getAuth(`${BASE_URL}/all`);
};

const updateProfile = async (payload) => {
    return httpClient.postAuth(`${BASE_URL}/update`, payload);
};

const getInvites = async () => {
    return httpClient.getAuth(`${BASE_URL}/invites`);
};

const acceptInvite = async (universityName) => {
    return httpClient.putAuth(`${BASE_URL}/invites/${encodeURIComponent(universityName)}/accept`, {});
};

const rejectInvite = async (universityName) => {
    return httpClient.deleteAuth(`${BASE_URL}/invites/${encodeURIComponent(universityName)}/reject`);
};

const enterUniversityAsStudent = async (universityName) => {
    return httpClient.postAuth(`${STUDENT_BASE_URL}/entry/${encodeURIComponent(universityName)}`, {});
};

export default {
    getAllUsers,
    getByName,
    getInvites,
    acceptInvite,
    rejectInvite,
    enterUniversityAsStudent,
    updateProfile,
};
