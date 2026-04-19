<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import courseApi from '@/Courses/services/courseApi';
import lessonResourcesService from '@/Courses/services/lessonResourcesService';
import LessonFormPanel from '@/Courses/components/LessonFormPanel.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const isMember = ref(false);
const course = ref(null);
const editingLesson = ref(null);
const editingLessonQuizQuestions = ref([]);
const editingLessonQuizAttemptPolicy = ref('reattempt');

const isLoadingWorkspace = ref(false);
const isJoining = ref(false);
const isSubmittingLesson = ref(false);

const actionMessage = ref('');
const errorMessage = ref('');
const formErrorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));
const lessonId = computed(() => Number(route.params.lessonId || 0));
const isEditingMode = computed(() => lessonId.value > 0);

const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const editingLessonResources = computed(() => lessonResourcesService.getLessonResources(editingLesson.value));
const pageTitle = computed(() => isEditingMode.value ? 'Edit lesson' : 'Create lesson');

function normalizeItems(payload) {
    return payload?.items || payload?.Items || payload?.values || payload?.Values || [];
}

function normalizeValue(payload) {
    return payload?.value || payload?.Value || null;
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function normalizeQuestionRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function normalizeAttemptPolicy(rawValue) {
    const raw = String(rawValue || 'reattempt').trim().toLowerCase();
    return raw === 'single' ? 'single' : 'reattempt';
}

function getQuizAttemptPolicy(questions) {
    const rows = Array.isArray(questions) ? questions : [];
    if (!rows.length) {
        return 'reattempt';
    }

    return normalizeAttemptPolicy(rows[0]?.attemptPolicy || rows[0]?.AttemptPolicy || 'reattempt');
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

async function loadUniversityMembership() {
    isLoadingWorkspace.value = true;
    clearMessages();

    try {
        authUtils.getCurrentUser();

        const [universityResponse, myUniversitiesResponse] = await Promise.all([
            universityApi.getUniversityByName(universityName.value),
            universityApi.getMyUniversities({ page: 1, pageSize: 100 }),
        ]);

        university.value = normalizeValue(universityResponse);

        const myItems = normalizeItems(myUniversitiesResponse);
        isMember.value = myItems.some((item) => String(item.name || item.Name || '').toLowerCase() === universityName.value.toLowerCase());
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load university workspace.';
        university.value = null;
        isMember.value = false;
    } finally {
        isLoadingWorkspace.value = false;
    }
}

async function loadCourse() {
    if (!courseId.value || !isMember.value) {
        course.value = null;
        return;
    }

    try {
        const response = await courseApi.getCourseById(courseId.value);
        course.value = normalizeValue(response);
    } catch {
        course.value = null;
    }
}

async function loadEditingLesson() {
    if (!isEditingMode.value || !lessonId.value || !canManage.value) {
        editingLesson.value = null;
        editingLessonQuizQuestions.value = [];
        editingLessonQuizAttemptPolicy.value = 'reattempt';
        return;
    }

    try {
        const [lessonResponse, quizResponse] = await Promise.all([
            courseApi.getLessonById(lessonId.value),
            courseApi.getLessonQuizQuestions(lessonId.value),
        ]);

        editingLesson.value = normalizeValue(lessonResponse);
        editingLessonQuizQuestions.value = normalizeQuestionRows(quizResponse);
        editingLessonQuizAttemptPolicy.value = getQuizAttemptPolicy(editingLessonQuizQuestions.value);
    } catch (error) {
        editingLesson.value = null;
        editingLessonQuizQuestions.value = [];
        editingLessonQuizAttemptPolicy.value = 'reattempt';
        errorMessage.value = error?.message || 'Failed to load lesson for editing.';
    }
}

async function loadWorkspace() {
    await loadUniversityMembership();

    if (!isMember.value) {
        return;
    }

    await loadCourse();

    if (canManage.value) {
        await loadEditingLesson();
    }
}

async function joinUniversity() {
    if (!university.value?.isOpened || isMember.value) {
        return;
    }

    isJoining.value = true;
    clearMessages();

    try {
        const response = await universityApi.enterUniversity(universityName.value);
        actionMessage.value = readMessage(response) || 'You entered the university as student.';
        isMember.value = true;
        await loadCourse();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

function openLessonsList() {
    router.push({
        name: 'UniversityCourseLessons',
        params: {
            name: universityName.value,
            courseId: courseId.value,
        },
    });
}

function openLessonDetail(id = lessonId.value) {
    const targetLessonId = Number(id || 0);
    if (!targetLessonId) {
        openLessonsList();
        return;
    }

    router.push({
        name: 'UniversityCourseLessonDetail',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId: targetLessonId,
        },
    });
}

async function upsertLessonQuizForLesson(targetLessonId, quizPayload, attemptPolicy) {
    if (!targetLessonId) {
        return;
    }

    await courseApi.upsertLessonQuiz({
        lessonId: targetLessonId,
        questions: Array.isArray(quizPayload) ? quizPayload : [],
        attemptPolicy: normalizeAttemptPolicy(attemptPolicy),
    });
}

async function resolveCreatedLessonIdByOrderNumber(orderNumber) {
    const response = await courseApi.getCourseLessons(courseId.value);
    const rows = normalizeItems(response);

    const target = rows.find((item) => Number(item?.orderNumber || item?.OrderNumber || 0) === Number(orderNumber || 0));
    if (target) {
        return Number(target?.id || target?.Id || 0);
    }

    if (!rows.length) {
        return 0;
    }

    const last = [...rows].sort((a, b) => Number(a?.id || a?.Id || 0) - Number(b?.id || b?.Id || 0)).at(-1);
    return Number(last?.id || last?.Id || 0);
}

async function submitLessonForm(payload) {
    formErrorMessage.value = payload.validationError || '';
    if (payload.validationError) {
        return;
    }

    clearMessages();
    isSubmittingLesson.value = true;

    try {
        const resourceFields = lessonResourcesService.buildApiResourceFields(payload.resources);
        const lessonQuizPayload = payload.lessonQuizQuestions;
        const lessonQuizAttemptPolicy = normalizeAttemptPolicy(payload.lessonQuizAttemptPolicy);

        if (isEditingMode.value) {
            const response = await courseApi.updateLesson({
                lessonId: lessonId.value,
                title: payload.title,
                content: payload.content,
                videoLink: resourceFields.videoLink,
                materialLink: resourceFields.materialLink,
                resources: resourceFields.resources,
                orderNumber: payload.orderNumber,
            });

            await upsertLessonQuizForLesson(lessonId.value, lessonQuizPayload, lessonQuizAttemptPolicy);

            actionMessage.value = readMessage(response) || 'Lesson updated successfully.';
            openLessonDetail();
            return;
        }

        const response = await courseApi.createLesson({
            courseId: courseId.value,
            title: payload.title,
            content: payload.content,
            isMarkdown: payload.isMarkdown,
            videoLink: resourceFields.videoLink,
            materialLink: resourceFields.materialLink,
            resources: resourceFields.resources,
            orderNumber: payload.orderNumber,
        });

        const createdLessonId = await resolveCreatedLessonIdByOrderNumber(payload.orderNumber);
        if (createdLessonId) {
            await upsertLessonQuizForLesson(createdLessonId, lessonQuizPayload, lessonQuizAttemptPolicy);
            actionMessage.value = readMessage(response) || 'Lesson created successfully.';
            openLessonDetail(createdLessonId);
            return;
        }

        actionMessage.value = readMessage(response) || 'Lesson created successfully.';
        openLessonsList();
    } catch (error) {
        formErrorMessage.value = error?.message || 'Could not save lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

async function removeLesson() {
    if (!isEditingMode.value || !lessonId.value || !canManage.value || isSubmittingLesson.value) {
        return;
    }

    clearMessages();
    isSubmittingLesson.value = true;

    try {
        const response = await courseApi.deleteLesson(lessonId.value);
        actionMessage.value = readMessage(response) || 'Lesson deleted successfully.';
        openLessonsList();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not delete lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

onMounted(async () => {
    authUtils.getCurrentUser();
    await loadWorkspace();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="courses"
            :subtitle="pageTitle"
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card edit-card">
            <div class="edit-card__actions">
                <button class="secondary-button" type="button" @click="openLessonsList">Back to lessons</button>
                <button v-if="isEditingMode" class="secondary-button" type="button" @click="openLessonDetail">View lesson</button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <div v-if="canManage" class="edit-card__body">
                <div>
                    <p class="section-kicker">{{ course?.title || course?.Title || 'Course' }}</p>
                    <h2 class="section-title">{{ pageTitle }}</h2>
                </div>

                <LessonFormPanel
                    :mode="isEditingMode ? 'edit' : 'create'"
                    :initial-lesson="editingLesson"
                    :initial-resources="editingLessonResources"
                    :initial-quiz-questions="editingLessonQuizQuestions"
                    :initial-quiz-attempt-policy="editingLessonQuizAttemptPolicy"
                    :is-submitting="isSubmittingLesson"
                    :error-message="formErrorMessage"
                    @submit="submitLessonForm"
                    @cancel="openLessonsList"
                />

                <button v-if="isEditingMode" class="secondary-button edit-card__delete" type="button" :disabled="isSubmittingLesson" @click="removeLesson">
                    Delete lesson
                </button>
            </div>

            <p v-else-if="isMember" class="state-message">Only teachers or directors can edit lessons.</p>
            <p v-else class="state-message">Membership is required to manage lessons in this course.</p>
        </section>
    </main>
</template>

<style scoped>
.page-shell {
    width: min(760px, calc(100% - 1.2rem));
    margin: 1rem auto 1.4rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.edit-card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.edit-card__actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.55rem;
}

.edit-card__body {
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.edit-card__delete {
    color: var(--ttl-danger);
    border: 1px solid rgba(213, 75, 90, 0.26);
}

.state-message {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.state-message--error {
    color: var(--ttl-danger);
}

.state-message--success {
    color: var(--ttl-success);
}

@media (max-width: 520px) {
    .edit-card__actions {
        grid-template-columns: 1fr;
    }
}
</style>
