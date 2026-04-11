import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const getRole = async (email) => {
	return httpClient.get(`${SERVICE_BASE_PATHS.users}/general/${email}`);
};

const becomeTeacher = async () => {
	return httpClient.postAuth(`${SERVICE_BASE_PATHS.users}/teacher/become`, {});
};

const becomeStudent = async () => {
	return httpClient.postAuth(`${SERVICE_BASE_PATHS.users}/student/become`, {});
};

const verifyTeacher = async (degree) => {
	return httpClient.postAuth(`${SERVICE_BASE_PATHS.users}/teacher/verify`, degree, {
		headers: {
			'Content-Type': 'application/json',
		},
	});
};

export default {
	getRole,
	becomeTeacher,
	becomeStudent,
	verifyTeacher,
};

