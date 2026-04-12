import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.courses}/courses`;

const getUniversityCourses = async (universityName, { page = 0 } = {}) => {
    return httpClient.getAuth(`${BASE_URL}/${encodeURIComponent(universityName)}/${page}`);
};

const getCourseById = async (courseId) => {
    return httpClient.getAuth(`${BASE_URL}/course/${courseId}`);
};

const createCourse = async ({ title, description, universityName }) => {
    return httpClient.postAuth(`${BASE_URL}/create`, {
        title,
        description,
        universityName,
    });
};

const updateCourse = async ({ courseId, title, description }) => {
    return httpClient.putAuth(`${BASE_URL}/update`, {
        courseId,
        title,
        description,
    });
};

export default {
    getUniversityCourses,
    getCourseById,
    createCourse,
    updateCourse,
};
