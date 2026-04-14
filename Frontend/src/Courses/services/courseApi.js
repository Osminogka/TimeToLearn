import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.courses}/courses`;
const LESSONS_BASE_URL = `${SERVICE_BASE_PATHS.courses}/lessons`;

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

const deleteCourse = async (courseId) => {
    return httpClient.deleteAuth(`${BASE_URL}/delete/${courseId}`);
};

const getCourseLessons = async (courseId) => {
    return httpClient.getAuth(`${LESSONS_BASE_URL}/course/${courseId}`);
};

const getLessonById = async (lessonId) => {
    return httpClient.getAuth(`${LESSONS_BASE_URL}/lesson/${lessonId}`);
};

const createLesson = async ({ courseId, title, content, isMarkdown, videoLink, materialLink, resources, orderNumber }) => {
    return httpClient.postAuth(`${LESSONS_BASE_URL}/create`, {
        courseId,
        title,
        content,
        isMarkdown,
        videoLink,
        materialLink,
        resources,
        orderNumber,
    });
};

const updateLesson = async ({ lessonId, title, content, videoLink, materialLink, resources, orderNumber }) => {
    return httpClient.putAuth(`${LESSONS_BASE_URL}/update`, {
        lessonId,
        title,
        content,
        videoLink,
        materialLink,
        resources,
        orderNumber,
    });
};

const deleteLesson = async (lessonId) => {
    return httpClient.deleteAuth(`${LESSONS_BASE_URL}/delete/${lessonId}`);
};

const reorderLessons = async ({ courseId, items }) => {
    return httpClient.putAuth(`${LESSONS_BASE_URL}/reorder`, {
        courseId,
        items,
    });
};

export default {
    getUniversityCourses,
    getCourseById,
    createCourse,
    updateCourse,
    deleteCourse,
    getCourseLessons,
    getLessonById,
    createLesson,
    updateLesson,
    deleteLesson,
    reorderLessons,
};
