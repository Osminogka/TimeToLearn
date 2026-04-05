import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const getRole = async (email) => {
	return httpClient.get(`${SERVICE_BASE_PATHS.users}/general/${email}`);
};

export default {
	getRole,
};

