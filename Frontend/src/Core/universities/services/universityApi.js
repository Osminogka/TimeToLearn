import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.users}/university`;
const STUDENT_BASE_URL = `${SERVICE_BASE_PATHS.users}/student`;
const TEACHER_BASE_URL = `${SERVICE_BASE_PATHS.users}/teacher`;
const DIRECTOR_BASE_URL = `${SERVICE_BASE_PATHS.users}/director`;

const encodeName = (name) => encodeURIComponent(name);

const getUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.get(`${BASE_URL}?page=${page}&pageSize=${pageSize}`);
};

const getMyUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.getAuth(`${BASE_URL}/my?page=${page}&pageSize=${pageSize}`);
};

const getAvailableUniversities = ({ page = 1, pageSize = 12 } = {}) => {
	return httpClient.getAuth(`${BASE_URL}/catalog/available?page=${page}&pageSize=${pageSize}`);
};

const getUniversityByName = (name) => {
	return httpClient.getAuth(`${BASE_URL}/${encodeName(name)}`);
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

const inviteStudent = (universityName, username) => {
	return httpClient.postAuth(`${DIRECTOR_BASE_URL}/invites/student`, {
		university: universityName,
		username,
	});
};

const inviteTeacher = (universityName, username) => {
	return httpClient.postAuth(`${DIRECTOR_BASE_URL}/invites/teacher`, {
		university: universityName,
		username,
	});
};

const removeMember = (universityName, username) => {
	return httpClient.deleteAuth(`${DIRECTOR_BASE_URL}/members/remove`, {
		university: universityName,
		username,
	});
};

const updateUniversityInfo = ({ name, description, address, isOpened }) => {
	return httpClient.postAuth(`${DIRECTOR_BASE_URL}/update`, {
		name,
		description,
		address,
		isOpened,
	});
};

const createUniversity = (payload) => {
	return httpClient.postAuth(BASE_URL, payload);
};

export default {
	getUniversities,
	getMyUniversities,
	getAvailableUniversities,
	getUniversityByName,
	getUniversityTeachers,
	getUniversityStudents,
	enterUniversity,
	requestJoinAsTeacher,
	inviteStudent,
	inviteTeacher,
	removeMember,
	updateUniversityInfo,
	createUniversity,
};

