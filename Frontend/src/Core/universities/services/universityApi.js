import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/university`;

const getUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.get(`${BASE_URL}?page=${page}&pageSize=${pageSize}`);
};

const getMyUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.get(`${BASE_URL}/my?page=${page}&pageSize=${pageSize}`);
};

const getUniversityByName = (name) => {
	return httpClient.get(`${BASE_URL}/${encodeURIComponent(name)}`);
};

const createUniversity = (payload) => {
	return httpClient.post(BASE_URL, payload);
};

export default {
	getUniversities,
	getMyUniversities,
	getUniversityByName,
	createUniversity,
};

