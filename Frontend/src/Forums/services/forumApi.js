import { httpClient } from '@/Shared/api/httpClient';
import { SERVICE_BASE_PATHS } from '@/Shared/api/serviceBasePaths';

const TOPIC_BASE_URL = `${SERVICE_BASE_PATHS.forums}/topics`;
const COMMENT_BASE_URL = `${SERVICE_BASE_PATHS.forums}/comments`;

const encodeName = (value) => encodeURIComponent(String(value || ''));

const getUniversityTopics = (universityName, { page = 0 } = {}) => {
    return httpClient.getAuth(`${TOPIC_BASE_URL}/${encodeName(universityName)}/${page}`);
};

const createTopic = ({ topicTitle, topicContent, universityName }) => {
    return httpClient.postAuth(`${TOPIC_BASE_URL}/create`, {
        topicTitle,
        topicContent,
        universityName,
    });
};

const likeTopic = (topicId) => {
    return httpClient.postAuth(`${TOPIC_BASE_URL}/like/${topicId}`, {});
};

const dislikeTopic = (topicId) => {
    return httpClient.postAuth(`${TOPIC_BASE_URL}/dislike/${topicId}`, {});
};

const getComments = ({ isTopic, recordId, page = 0 }) => {
    return httpClient.getAuth(`${COMMENT_BASE_URL}/${Boolean(isTopic)}/${recordId}/${page}`);
};

const createComment = ({ postId, isTopic, universityName, commentContent }) => {
    return httpClient.postAuth(`${COMMENT_BASE_URL}/create`, {
        postId,
        isTopic,
        universityName,
        commentContent,
    });
};

const likeComment = (commentId) => {
    return httpClient.postAuth(`${COMMENT_BASE_URL}/like/${commentId}`, {});
};

const dislikeComment = (commentId) => {
    return httpClient.postAuth(`${COMMENT_BASE_URL}/dislike/${commentId}`, {});
};

export default {
    getUniversityTopics,
    createTopic,
    likeTopic,
    dislikeTopic,
    getComments,
    createComment,
    likeComment,
    dislikeComment,
};
