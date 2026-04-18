import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const BASE_URL = `${SERVICE_BASE_PATHS.courses}/courses`;
const LESSONS_BASE_URL = `${SERVICE_BASE_PATHS.courses}/lessons`;
const QUIZZES_BASE_URL = `${SERVICE_BASE_PATHS.courses}/quizzes`;

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

const completeLesson = async (lessonId) => {
    return httpClient.postAuth(`${LESSONS_BASE_URL}/${lessonId}/complete`, {});
};

const getLessonProgress = async (lessonId) => {
    return httpClient.getAuth(`${LESSONS_BASE_URL}/${lessonId}/progress`);
};

const getCourseProgress = async (courseId) => {
    return httpClient.getAuth(`${BASE_URL}/${courseId}/progress`);
};

const getCourseStudentsProgress = async (courseId) => {
    return httpClient.getAuth(`${BASE_URL}/${courseId}/student-progress`);
};

const assignCourseGrade = async ({ courseId, studentId, mark }) => {
    return httpClient.postAuth(`${BASE_URL}/${courseId}/grade-student`, {
        studentId,
        mark,
    });
};

const getLessonQuizQuestions = async (lessonId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/lesson/${lessonId}`);
};

const getCourseQuizQuestions = async (courseId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/course/${courseId}`);
};

const createLessonQuizQuestion = async ({ lessonId, questionText, options, attemptPolicy }) => {
    return httpClient.postAuth(`${QUIZZES_BASE_URL}/lesson/${lessonId}/questions`, {
        questionText,
        options,
        attemptPolicy,
    });
};

const createCourseQuizQuestion = async ({ courseId, questionText, options, attemptPolicy }) => {
    return httpClient.postAuth(`${QUIZZES_BASE_URL}/course/${courseId}/questions`, {
        questionText,
        options,
        attemptPolicy,
    });
};

const submitLessonQuizAnswer = async ({ lessonId, questionId, selectedOptionId }) => {
    return httpClient.postAuth(`${QUIZZES_BASE_URL}/lesson/${lessonId}/questions/${questionId}/answer`, {
        selectedOptionId,
    });
};

const submitCourseQuizAnswer = async ({ courseId, questionId, selectedOptionId }) => {
    return httpClient.postAuth(`${QUIZZES_BASE_URL}/course/${courseId}/questions/${questionId}/answer`, {
        selectedOptionId,
    });
};

const getMyLessonQuizAnswers = async (lessonId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/lesson/${lessonId}/my-answers`);
};

const getMyCourseQuizAnswers = async (courseId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/course/${courseId}/my-answers`);
};

const getLessonQuizAnswersForTeacher = async (lessonId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/lesson/${lessonId}/answers`);
};

const getCourseQuizAnswersForTeacher = async (courseId) => {
    return httpClient.getAuth(`${QUIZZES_BASE_URL}/course/${courseId}/answers`);
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
    completeLesson,
    getLessonProgress,
    getCourseProgress,
    getCourseStudentsProgress,
    assignCourseGrade,
    getLessonQuizQuestions,
    getCourseQuizQuestions,
    createLessonQuizQuestion,
    createCourseQuizQuestion,
    submitLessonQuizAnswer,
    submitCourseQuizAnswer,
    getMyLessonQuizAnswers,
    getMyCourseQuizAnswers,
    getLessonQuizAnswersForTeacher,
    getCourseQuizAnswersForTeacher,
};
