<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import courseApi from '@/Courses/services/courseApi';
import lessonResourcesService from '@/Courses/services/lessonResourcesService';
import markdownService from '@/Courses/services/markdownService';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const course = ref(null);
const lessons = ref([]);
const selectedLesson = ref(null);

const isMember = ref(false);
const isLoadingWorkspace = ref(false);
const isLoadingLesson = ref(false);
const isJoining = ref(false);
const isCompletingLesson = ref(false);
const isLoadingLessonQuiz = ref(false);
const isLoadingStudentQuizAnswers = ref(false);
const isSubmittingLessonQuiz = ref(false);

const lessonQuizQuestions = ref([]);
const myLessonQuizAnswers = ref([]);
const lessonQuizAnswerDraftByQuestion = ref({});

const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));
const lessonId = computed(() => Number(route.params.lessonId || 0));

const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const canView = computed(() => isMember.value);
const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const hasLessonQuizQuestions = computed(() => lessonQuizQuestions.value.length > 0);
const selectedLessonResources = computed(() => lessonResourcesService.getLessonResources(selectedLesson.value));

const isSelectedLessonMarkdown = computed(() => {
    const raw = selectedLesson.value?.isMarkdown ?? selectedLesson.value?.IsMarkdown;
    return Boolean(raw);
});

const selectedLessonCompleted = computed(() => {
    const raw = selectedLesson.value?.isCompletedByCurrentUser ?? selectedLesson.value?.IsCompletedByCurrentUser;
    return Boolean(raw);
});

const canCompleteSelectedLesson = computed(() => {
    if (!selectedLesson.value || canManage.value || isCompletingLesson.value) {
        return false;
    }

    return !selectedLessonCompleted.value;
});

const selectedLessonIndex = computed(() => lessons.value.findIndex((item) => getLessonId(item) === lessonId.value));
const previousLesson = computed(() => selectedLessonIndex.value > 0 ? lessons.value[selectedLessonIndex.value - 1] : null);
const nextLesson = computed(() => selectedLessonIndex.value >= 0 && selectedLessonIndex.value < lessons.value.length - 1 ? lessons.value[selectedLessonIndex.value + 1] : null);

const myLessonQuizCorrectCount = computed(() => myLessonQuizAnswers.value.filter(item => item.isCorrect || item.IsCorrect).length);
const myLessonQuizScoreLabel = computed(() => `${myLessonQuizCorrectCount.value}/${myLessonQuizAnswers.value.length}`);

function normalizeItems(payload) {
    return payload?.items || payload?.Items || payload?.values || payload?.Values || [];
}

function normalizeValue(payload) {
    return payload?.value || payload?.Value || null;
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function getLessonId(item) {
    return Number(item?.id || item?.Id || 0);
}

function getQuestionId(item) {
    return Number(item?.id || item?.Id || 0);
}

function getOptionId(item) {
    return Number(item?.id || item?.Id || 0);
}

function normalizeQuestionRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function normalizeAnswerRows(payload) {
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

function isSingleAttemptLessonQuiz() {
    return getQuizAttemptPolicy(lessonQuizQuestions.value) === 'single';
}

function canEditLessonQuizAnswers() {
    if (canManage.value) {
        return false;
    }

    return !(isSingleAttemptLessonQuiz() && myLessonQuizAnswers.value.length > 0);
}

function getExistingAnswerOptionId(questionIdValue, answers) {
    const row = answers.find(item => Number(item?.questionId || item?.QuestionId) === questionIdValue);
    if (!row) {
        return 0;
    }

    return Number(row?.selectedOptionId || row?.SelectedOptionId || 0);
}

function hasSelectedAnswersForAllQuestions(questions, drafts) {
    const rows = Array.isArray(questions) ? questions : [];
    if (!rows.length) {
        return false;
    }

    return rows.every((question) => {
        const id = getQuestionId(question);
        return id > 0 && Number(drafts?.[id] || 0) > 0;
    });
}

function buildSubmitAnswersPayload(questions, drafts) {
    return (Array.isArray(questions) ? questions : []).map((question) => ({
        questionId: getQuestionId(question),
        selectedOptionId: Number(drafts?.[getQuestionId(question)] || 0),
    }));
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

function sortLessons(rows) {
    return [...rows].sort((a, b) => {
        const orderA = Number(a?.orderNumber || a?.OrderNumber || 0);
        const orderB = Number(b?.orderNumber || b?.OrderNumber || 0);
        return orderA - orderB;
    });
}

function getPreviewContent(item) {
    const rawContent = lessonResourcesService.removeEmbeddedResourcesFromContent(item?.content || item?.Content || '');

    if (item?.isMarkdown ?? item?.IsMarkdown) {
        return markdownService.renderMarkdownToSafeHtml(rawContent);
    }

    return rawContent;
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

async function loadCourseAndLessons() {
    if (!canView.value || !courseId.value) {
        course.value = null;
        lessons.value = [];
        return;
    }

    try {
        const [courseResponse, lessonsResponse] = await Promise.all([
            courseApi.getCourseById(courseId.value),
            courseApi.getCourseLessons(courseId.value),
        ]);

        course.value = normalizeValue(courseResponse);
        lessons.value = sortLessons(normalizeItems(lessonsResponse));
    } catch (error) {
        course.value = null;
        lessons.value = [];
        errorMessage.value = error?.message || 'Failed to load course lesson list.';
    }
}

async function loadLessonDetails() {
    if (!canView.value || !lessonId.value) {
        selectedLesson.value = null;
        return;
    }

    isLoadingLesson.value = true;

    try {
        const response = await courseApi.getLessonById(lessonId.value);
        selectedLesson.value = normalizeValue(response);
    } catch (error) {
        selectedLesson.value = null;
        errorMessage.value = error?.message || 'Failed to load lesson details.';
    } finally {
        isLoadingLesson.value = false;
    }
}

async function loadSelectedLessonQuiz() {
    if (!canView.value || !lessonId.value) {
        lessonQuizQuestions.value = [];
        return;
    }

    isLoadingLessonQuiz.value = true;
    try {
        const response = await courseApi.getLessonQuizQuestions(lessonId.value);
        lessonQuizQuestions.value = normalizeQuestionRows(response);
    } catch {
        lessonQuizQuestions.value = [];
    } finally {
        isLoadingLessonQuiz.value = false;
    }
}

async function loadMyQuizAnswers() {
    if (!canView.value || canManage.value || !lessonId.value) {
        myLessonQuizAnswers.value = [];
        lessonQuizAnswerDraftByQuestion.value = {};
        return;
    }

    isLoadingStudentQuizAnswers.value = true;

    try {
        const lessonResponse = await courseApi.getMyLessonQuizAnswers(lessonId.value);
        myLessonQuizAnswers.value = normalizeAnswerRows(lessonResponse);

        lessonQuizAnswerDraftByQuestion.value = lessonQuizQuestions.value.reduce((acc, item) => {
            const questionIdValue = getQuestionId(item);
            acc[questionIdValue] = getExistingAnswerOptionId(questionIdValue, myLessonQuizAnswers.value);
            return acc;
        }, {});
    } catch {
        myLessonQuizAnswers.value = [];
        lessonQuizAnswerDraftByQuestion.value = {};
    } finally {
        isLoadingStudentQuizAnswers.value = false;
    }
}

async function loadWorkspace() {
    await loadUniversityMembership();

    if (!canView.value) {
        return;
    }

    await loadCourseAndLessons();
    await loadLessonDetails();
    await loadSelectedLessonQuiz();
    await loadMyQuizAnswers();
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
        await loadCourseAndLessons();
        await loadLessonDetails();
        await loadSelectedLessonQuiz();
        await loadMyQuizAnswers();
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

function openEditLesson() {
    if (!lessonId.value || !canManage.value) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonEdit',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId: lessonId.value,
        },
    });
}

function openLessonById(targetId) {
    const id = Number(targetId || 0);
    if (!id) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonDetail',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId: id,
        },
    });
}

async function completeSelectedLesson() {
    if (!lessonId.value || !canCompleteSelectedLesson.value) {
        return;
    }

    clearMessages();
    isCompletingLesson.value = true;

    try {
        const response = await courseApi.completeLesson(lessonId.value);
        actionMessage.value = readMessage(response) || 'Lesson marked as completed.';

        await loadCourseAndLessons();
        await loadLessonDetails();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not complete lesson.';
    } finally {
        isCompletingLesson.value = false;
    }
}

async function submitLessonQuiz() {
    if (!lessonId.value || isSubmittingLessonQuiz.value || !canEditLessonQuizAnswers()) {
        return;
    }

    if (!hasSelectedAnswersForAllQuestions(lessonQuizQuestions.value, lessonQuizAnswerDraftByQuestion.value)) {
        errorMessage.value = 'Please select answers for all lesson quiz questions before submitting.';
        return;
    }

    const answers = buildSubmitAnswersPayload(lessonQuizQuestions.value, lessonQuizAnswerDraftByQuestion.value);

    clearMessages();
    isSubmittingLessonQuiz.value = true;
    try {
        const response = await courseApi.submitLessonQuiz({
            lessonId: lessonId.value,
            answers,
        });

        actionMessage.value = readMessage(response) || 'Lesson mini quiz submitted.';
        await loadMyQuizAnswers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not submit lesson mini quiz.';
    } finally {
        isSubmittingLessonQuiz.value = false;
    }
}

watch(
    () => route.params.lessonId,
    async () => {
        if (!canView.value) {
            return;
        }

        await loadLessonDetails();
        await loadSelectedLessonQuiz();
        await loadMyQuizAnswers();
    }
);

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
            subtitle="Lesson details"
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card detail-card">
            <div class="detail-card__actions">
                <button class="secondary-button" type="button" @click="openLessonsList">Back to lessons</button>
                <button v-if="canManage" class="secondary-button" type="button" @click="openEditLesson">Edit lesson</button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article v-if="canView && selectedLesson" class="detail-card__body">
                <div>
                    <p class="section-kicker">{{ course?.title || course?.Title || 'Course' }}</p>
                    <h2 class="section-title">{{ selectedLesson.title || selectedLesson.Title }}</h2>
                </div>

                <div
                    v-if="isSelectedLessonMarkdown"
                    class="lesson-content lesson-content--markdown section-copy"
                    v-html="getPreviewContent(selectedLesson)"
                />
                <p v-else class="lesson-content lesson-content--plain section-copy">{{ getPreviewContent(selectedLesson) }}</p>

                <div v-if="selectedLessonResources.length" class="resource-list">
                    <a
                        v-for="resource in selectedLessonResources"
                        :key="resource.url"
                        class="secondary-button resource-list__item"
                        :href="resource.url"
                        target="_blank"
                        rel="noreferrer noopener"
                    >
                        {{ resource.title }}
                    </a>
                </div>

                <div v-if="!canManage" class="completion-card surface-card">
                    <p class="section-copy">{{ selectedLessonCompleted ? 'You have completed this lesson.' : 'Mark this lesson as complete after reading it.' }}</p>
                    <button class="submit-button" type="button" :disabled="!canCompleteSelectedLesson" @click="completeSelectedLesson">
                        {{ isCompletingLesson ? 'Saving...' : selectedLessonCompleted ? 'Completed' : 'Mark as complete' }}
                    </button>
                </div>

                <section v-if="hasLessonQuizQuestions || canManage" class="quiz-card surface-card">
                    <div class="quiz-card__head">
                        <p class="section-kicker">Lesson mini quiz</p>
                        <p v-if="hasLessonQuizQuestions" class="quiz-card__policy">Policy: {{ isSingleAttemptLessonQuiz() ? 'Single attempt' : 'Reattempt allowed' }}</p>
                    </div>

                    <p v-if="!hasLessonQuizQuestions && !isLoadingLessonQuiz" class="state-message">No lesson mini quiz questions published yet.</p>

                    <div v-if="hasLessonQuizQuestions" class="quiz-list">
                        <article v-for="question in lessonQuizQuestions" :key="getQuestionId(question)" class="quiz-item">
                            <p class="quiz-item__question">{{ question.questionText || question.QuestionText }}</p>

                            <label v-for="option in question.options || question.Options || []" :key="getOptionId(option)" class="quiz-item__option">
                                <input
                                    v-model.number="lessonQuizAnswerDraftByQuestion[getQuestionId(question)]"
                                    :disabled="!canEditLessonQuizAnswers() || isSubmittingLessonQuiz"
                                    type="radio"
                                    :name="`lesson-question-${getQuestionId(question)}`"
                                    :value="getOptionId(option)"
                                />
                                <span>{{ option.optionText || option.OptionText }}</span>
                                <span v-if="canManage && (option.isCorrect ?? option.IsCorrect)" class="pill pill--accent">Correct</span>
                            </label>
                        </article>
                    </div>

                    <button
                        v-if="!canManage && hasLessonQuizQuestions"
                        class="submit-button"
                        type="button"
                        :disabled="isSubmittingLessonQuiz || !hasSelectedAnswersForAllQuestions(lessonQuizQuestions, lessonQuizAnswerDraftByQuestion) || !canEditLessonQuizAnswers()"
                        @click="submitLessonQuiz"
                    >
                        {{ isSubmittingLessonQuiz ? 'Submitting...' : 'Submit quiz' }}
                    </button>

                    <p v-if="!canManage && isSingleAttemptLessonQuiz() && myLessonQuizAnswers.length" class="state-message quiz-card__lock">
                        Quiz is locked after your first submit.
                    </p>

                    <p v-if="!canManage && myLessonQuizAnswers.length" class="quiz-card__score">Score: {{ myLessonQuizScoreLabel }}</p>
                </section>

                <div class="pager-actions">
                    <button class="secondary-button" type="button" :disabled="!previousLesson" @click="openLessonById(getLessonId(previousLesson))">Previous</button>
                    <button class="secondary-button" type="button" :disabled="!nextLesson" @click="openLessonById(getLessonId(nextLesson))">Next</button>
                </div>
            </article>

            <p v-else-if="canView && isLoadingLesson" class="state-message">Loading lesson...</p>
            <p v-else-if="canView" class="state-message">Lesson not found.</p>
            <p v-else class="state-message">Membership is required to view this lesson.</p>
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

.detail-card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.detail-card__actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.55rem;
}

.detail-card__body {
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.lesson-content {
    margin: 0;
    line-height: 1.7;
}

.lesson-content--plain {
    white-space: pre-wrap;
}

.lesson-content--markdown {
    white-space: normal;
}

.resource-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.resource-list__item {
    text-decoration: none;
    min-height: 2.5rem;
}

.completion-card,
.quiz-card {
    padding: 0.8rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.quiz-card__head {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.quiz-card__policy {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
}

.quiz-list {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.quiz-item {
    padding: 0.6rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-surface-soft);
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.quiz-item__question {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.quiz-item__option {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    color: var(--ttl-text-secondary);
}

.quiz-card__lock {
    color: var(--ttl-warning);
}

.quiz-card__score {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 800;
}

.pager-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.55rem;
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
    .detail-card__actions,
    .pager-actions {
        grid-template-columns: 1fr;
    }
}
</style>
