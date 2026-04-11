import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/university`;
const STUDENT_BASE_URL = `${SERVICE_BASE_PATHS.users}/student`;
const TEACHER_BASE_URL = `${SERVICE_BASE_PATHS.users}/teacher`;

const encodeName = (name) => encodeURIComponent(name);

const getUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.get(`${BASE_URL}?page=${page}&pageSize=${pageSize}`);
};

const getMyUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.getAuth(`${BASE_URL}/my?page=${page}&pageSize=${pageSize}`);
};

const getUniversityByName = (name) => {
	return httpClient.get(`${BASE_URL}/${encodeName(name)}`);
};

const getUniversityTeachers = (name, { page = 1, pageSize = 6 } = {}) => {
	return httpClient.getAuth(`${BASE_URL}/${encodeName(name)}/teachers?page=${page}&pageSize=${pageSize}`);
};

const getUniversityStudents = (name, { page = 1, pageSize = 6 } = {}) => {
	return httpClient.getAuth(`${BASE_URL}/${encodeName(name)}/students?page=${page}&pageSize=${pageSize}`);
};

const enterUniversity = (name) => {
	return httpClient.postAuth(`${STUDENT_BASE_URL}/entry/${encodeName(name)}`, {});
};

const requestJoinAsTeacher = (name) => {
	return httpClient.postAuth(`${TEACHER_BASE_URL}/request/${encodeName(name)}`, {});
};

const createUniversity = (payload) => {
	return httpClient.postAuth(BASE_URL, payload);
};

export default {
	getUniversities,
	getMyUniversities,
	getUniversityByName,
	getUniversityTeachers,
	getUniversityStudents,
	enterUniversity,
	requestJoinAsTeacher,
	createUniversity,
};

