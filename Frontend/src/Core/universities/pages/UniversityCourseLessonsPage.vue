<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';
import courseApi from '@/Courses/services/courseApi';
import lessonResourcesService from '@/Courses/services/lessonResourcesService';
import markdownService from '@/Courses/services/markdownService';
import LessonCard from '@/Courses/components/LessonCard.vue';
import LessonFormPanel from '@/Courses/components/LessonFormPanel.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const course = ref(null);
const lessons = ref([]);

const isLoadingWorkspace = ref(false);
const isLoadingLessons = ref(false);
const isSubmittingLesson = ref(false);
const isJoining = ref(false);

const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');
const formErrorMessage = ref('');

const isLessonFormOpen = ref(false);
const editingLessonId = ref(0);
const selectedLessonId = ref(0);
const selectedLesson = ref(null);
const visitedLessonIds = ref([]);

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));
const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const canView = computed(() => isMember.value);

const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const hasLessons = computed(() => lessons.value.length > 0);
const isEditingMode = computed(() => Boolean(editingLessonId.value));
const progressCompletedCount = computed(() => visitedLessonIds.value.length);
const progressTotalCount = computed(() => lessons.value.length);
const progressPercent = computed(() => {
    if (!progressTotalCount.value) {
        return 0;
    }

    return Math.round((progressCompletedCount.value / progressTotalCount.value) * 100);
});

const editingLesson = computed(() => {
    if (!editingLessonId.value) {
        return null;
    }

    return lessons.value.find((item) => Number(item.id || item.Id) === editingLessonId.value) || null;
});

const selectedLessonResolved = computed(() => {
    if (selectedLesson.value) {
        return selectedLesson.value;
    }

    if (!selectedLessonId.value) {
        return null;
    }

    return lessons.value.find((item) => Number(item.id || item.Id) === selectedLessonId.value) || null;
});

const selectedLessonResources = computed(() => lessonResourcesService.getLessonResources(selectedLessonResolved.value));

const editingLessonResources = computed(() => lessonResourcesService.getLessonResources(editingLesson.value));

const isSelectedLessonMarkdown = computed(() => {
    const raw = selectedLessonResolved.value?.isMarkdown ?? selectedLessonResolved.value?.IsMarkdown;
    return Boolean(raw);
});

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

function getCourseTitle() {
    return course.value?.title || course.value?.Title || 'Course';
}

function getProgressStorageKey() {
    return `ttl-progress:${String(user.value.email || '').toLowerCase()}:${universityName.value}:${courseId.value}`;
}

function loadProgress() {
    const key = getProgressStorageKey();
    const raw = localStorage.getItem(key);
    if (!raw) {
        visitedLessonIds.value = [];
        return;
    }

    try {
        const parsed = JSON.parse(raw);
        visitedLessonIds.value = Array.isArray(parsed)
            ? parsed.map((item) => Number(item)).filter((item) => Number.isInteger(item) && item > 0)
            : [];
    } catch {
        visitedLessonIds.value = [];
    }
}

function saveProgress() {
    localStorage.setItem(getProgressStorageKey(), JSON.stringify(visitedLessonIds.value));
}

function syncProgressWithLessons() {
    const lessonIds = new Set(lessons.value.map((item) => getLessonId(item)).filter((item) => item > 0));
    visitedLessonIds.value = visitedLessonIds.value.filter((item) => lessonIds.has(item));
    saveProgress();
}

function markLessonVisited(lessonId) {
    if (!lessonId) {
        return;
    }

    if (visitedLessonIds.value.includes(lessonId)) {
        return;
    }

    visitedLessonIds.value = [...visitedLessonIds.value, lessonId];
    saveProgress();
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

function resetLessonForm() {
    editingLessonId.value = 0;
    formErrorMessage.value = '';
    isLessonFormOpen.value = false;
}

function openCreateLessonForm() {
    editingLessonId.value = 0;
    formErrorMessage.value = '';
    isLessonFormOpen.value = true;
}

function openEditLessonForm(lesson) {
    const id = getLessonId(lesson);
    if (!id) {
        return;
    }

    editingLessonId.value = id;
    formErrorMessage.value = '';
    isLessonFormOpen.value = true;
}

function closeLessonForm() {
    resetLessonForm();
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

async function loadCourse() {
    if (!courseId.value || !canView.value) {
        course.value = null;
        return;
    }

    try {
        const response = await courseApi.getCourseById(courseId.value);
        course.value = normalizeValue(response);
    } catch (error) {
        course.value = null;
        errorMessage.value = error?.message || 'Failed to load course details.';
    }
}

async function loadLessons() {
    if (!courseId.value || !canView.value) {
        lessons.value = [];
        selectedLesson.value = null;
        selectedLessonId.value = 0;
        return;
    }

    isLoadingLessons.value = true;
    formErrorMessage.value = '';

    try {
        const response = await courseApi.getCourseLessons(courseId.value);
        lessons.value = normalizeItems(response);
        syncProgressWithLessons();

        if (!lessons.value.length) {
            selectedLessonId.value = 0;
            selectedLesson.value = null;
            return;
        }

        const firstId = getLessonId(lessons.value[0]);
        if (!selectedLessonId.value || !lessons.value.some((item) => getLessonId(item) === selectedLessonId.value)) {
            selectedLessonId.value = firstId;
            await loadLessonDetails(firstId);
            markLessonVisited(firstId);
        }
    } catch (error) {
        lessons.value = [];
        selectedLesson.value = null;
        selectedLessonId.value = 0;
        errorMessage.value = error?.message || 'Failed to load lessons.';
    } finally {
        isLoadingLessons.value = false;
    }
}

async function loadLessonDetails(lessonId) {
    if (!lessonId) {
        return;
    }

    try {
        const response = await courseApi.getLessonById(lessonId);
        selectedLesson.value = normalizeValue(response);
    } catch (error) {
        selectedLesson.value = null;
        errorMessage.value = error?.message || 'Failed to load lesson details.';
    }
}

async function refreshWorkspace() {
    await loadUniversityMembership();

    if (canView.value) {
        await loadCourse();
        await loadLessons();
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
        await loadLessons();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

async function selectLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId || lessonId === selectedLessonId.value) {
        return;
    }

    selectedLessonId.value = lessonId;
    selectedLesson.value = null;
    await loadLessonDetails(lessonId);
    markLessonVisited(lessonId);
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

        if (isEditingMode.value) {
            const response = await courseApi.updateLesson({
                lessonId: editingLessonId.value,
                title: payload.title,
                content: payload.content,
                videoLink: resourceFields.videoLink,
                materialLink: resourceFields.materialLink,
                resources: resourceFields.resources,
                orderNumber: payload.orderNumber,
            });

            actionMessage.value = readMessage(response) || 'Lesson updated successfully.';
            await loadLessons();
            if (selectedLessonId.value) {
                await loadLessonDetails(selectedLessonId.value);
            }
        } else {
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

            actionMessage.value = readMessage(response) || 'Lesson created successfully.';
            await loadLessons();

            if (lessons.value.length) {
                const newestLessonId = getLessonId(lessons.value[lessons.value.length - 1]);
                if (newestLessonId) {
                    selectedLessonId.value = newestLessonId;
                    await loadLessonDetails(newestLessonId);
                }
            }
        }

        resetLessonForm();
    } catch (error) {
        formErrorMessage.value = error?.message || 'Could not save lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

async function removeLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId || !canManage.value || isSubmittingLesson.value) {
        return;
    }

    clearMessages();
    isSubmittingLesson.value = true;

    try {
        const response = await courseApi.deleteLesson(lessonId);
        actionMessage.value = readMessage(response) || 'Lesson deleted successfully.';

        if (editingLessonId.value === lessonId) {
            resetLessonForm();
        }

        const deletedWasSelected = selectedLessonId.value === lessonId;
        await loadLessons();

        if (deletedWasSelected && lessons.value.length) {
            const fallbackId = getLessonId(lessons.value[0]);
            selectedLessonId.value = fallbackId;
            await loadLessonDetails(fallbackId);
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not delete lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

function openCoursesWorkspace() {
    router.push({ name: 'UniversityCourses', params: { name: universityName.value } });
}

onMounted(async () => {
    authUtils.getCurrentUser();
    loadProgress();
    await refreshWorkspace();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="courses"
            subtitle="Main study space for course work."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card workspace-content">
            <div class="workspace-content__heading-row">
                <div>
                    <p class="section-kicker">Course lessons</p>
                    <h2 class="section-title">{{ getCourseTitle() }}</h2>
                </div>

                <div class="workspace-content__actions">
                    <button class="secondary-button" type="button" @click="openCoursesWorkspace">Back to courses</button>
                    <button class="secondary-button" type="button" @click="refreshWorkspace" :disabled="isLoadingWorkspace || isLoadingLessons">
                        {{ isLoadingWorkspace || isLoadingLessons ? 'Refreshing...' : 'Refresh workspace' }}
                    </button>
                    <button
                        v-if="canManage"
                        class="submit-button"
                        type="button"
                        @click="openCreateLessonForm"
                        :disabled="isSubmittingLesson || isLoadingLessons"
                    >
                        Create lesson
                    </button>
                </div>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="hero-placeholder surface-card">
                <AppIcon name="courses" />
                <div>
                    <h3>{{ canView ? 'Lesson workspace' : 'Join to access lessons' }}</h3>
                    <p class="section-copy">
                        {{
                            canView
                                ? 'Read lesson content, open supplementary links, and keep learning flow clear through ordered lessons.'
                                : 'Membership is required to read lessons. Use join to enter this university as a student.'
                        }}
                    </p>
                </div>
            </article>

            <article v-if="canView && hasLessons" class="progress-card surface-card">
                <div class="progress-card__head">
                    <p class="section-kicker">Course progression</p>
                    <p class="progress-card__status">{{ progressCompletedCount }} / {{ progressTotalCount }} completed</p>
                </div>
                <div class="progress-card__track" role="progressbar" :aria-valuenow="progressPercent" aria-valuemin="0" aria-valuemax="100">
                    <div class="progress-card__fill" :style="{ width: `${progressPercent}%` }" />
                </div>
                <p class="section-copy progress-card__percent">{{ progressPercent }}% complete</p>
            </article>

            <LessonFormPanel
                v-if="isLessonFormOpen"
                :mode="isEditingMode ? 'edit' : 'create'"
                :initial-lesson="editingLesson"
                :initial-resources="editingLessonResources"
                :is-submitting="isSubmittingLesson"
                :error-message="formErrorMessage"
                @submit="submitLessonForm"
                @cancel="closeLessonForm"
            />

            <div v-if="canView" class="lesson-layout">
                <aside class="lesson-list surface-card">
                    <p class="section-kicker">Lessons in course</p>

                    <transition-group name="fade-rise" tag="div" class="lesson-list__items">
                        <LessonCard
                            v-for="item in lessons"
                            :key="item.id || item.Id"
                            :lesson="item"
                            :is-active="selectedLessonId === getLessonId(item)"
                            :can-manage="canManage"
                            :disable-actions="isSubmittingLesson || isLoadingLessons"
                            @open="selectLesson"
                            @edit="openEditLessonForm"
                            @delete="removeLesson"
                        />
                    </transition-group>

                    <p v-if="!isLoadingLessons && !hasLessons" class="state-message">
                        No lessons yet. {{ canManage ? 'Create the first lesson to start the course.' : 'A teacher will publish lessons soon.' }}
                    </p>
                </aside>

                <article class="lesson-view surface-card" v-if="selectedLessonResolved">
                    <div class="lesson-view__heading">
                        <div>
                            <p class="section-kicker">Selected lesson</p>
                            <h3>{{ selectedLessonResolved.title || selectedLessonResolved.Title }}</h3>
                        </div>
                        <span class="pill pill--accent">Order {{ selectedLessonResolved.orderNumber || selectedLessonResolved.OrderNumber }}</span>
                    </div>

                    <div
                        v-if="isSelectedLessonMarkdown"
                        class="lesson-view__content lesson-view__content--markdown section-copy"
                        v-html="getPreviewContent(selectedLessonResolved)"
                    />
                    <p v-else class="lesson-view__content lesson-view__content--plain section-copy">{{ getPreviewContent(selectedLessonResolved) }}</p>

                    <div class="lesson-view__links" v-if="selectedLessonResources.length">
                        <a
                            v-for="resource in selectedLessonResources"
                            :key="resource.url"
                            class="secondary-button lesson-view__link"
                            :href="resource.url"
                            target="_blank"
                            rel="noreferrer noopener"
                        >
                            {{ resource.title }}
                        </a>
                    </div>
                </article>

                <article class="lesson-view surface-card" v-else>
                    <p class="section-copy">Select a lesson to view full content.</p>
                </article>
            </div>

            <p v-else class="state-message">Membership is required to view lessons in this course.</p>
        </section>
    </main>
</template>

<style scoped>
.page-shell {
    width: min(1120px, calc(100% - 2rem));
    margin: 1.5rem auto 2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.workspace-content {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.workspace-content__heading-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.8rem;
}

.workspace-content__actions {
    display: flex;
    gap: 0.65rem;
    align-items: center;
    flex-wrap: wrap;
    justify-content: flex-end;
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

.hero-placeholder {
    padding: 1rem;
    display: flex;
    align-items: flex-start;
    gap: 0.8rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.06));
}

.hero-placeholder h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.hero-placeholder p {
    margin: 0.3rem 0 0;
    color: var(--ttl-text-secondary);
    line-height: 1.6;
}

.progress-card {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.progress-card__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.7rem;
}

.progress-card__status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.86rem;
}

.progress-card__track {
    height: 0.62rem;
    border-radius: 0.6rem;
    background: rgba(143, 44, 226, 0.12);
    overflow: hidden;
}

.progress-card__fill {
    height: 100%;
    border-radius: 0.6rem;
    background: linear-gradient(90deg, rgba(143, 44, 226, 0.85), rgba(255, 95, 162, 0.85));
    transition: width var(--ttl-transition-base);
}

.progress-card__percent {
    margin: 0;
}

.lesson-layout {
    display: grid;
    grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.25fr);
    gap: 0.9rem;
}

.lesson-list,
.lesson-view {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}

.lesson-list__items {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.lesson-view__heading {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.7rem;
}

.lesson-view__heading h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.lesson-view__content {
    margin: 0;
    line-height: 1.7;
}

.lesson-view__content--plain {
    white-space: pre-wrap;
}

.lesson-view__content--markdown {
    white-space: normal;
}

.lesson-view__links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.55rem;
}

.lesson-view__link {
    text-decoration: none;
    min-height: 2.25rem;
}

@media (max-width: 960px) {
    .lesson-layout {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 700px) {
    .workspace-content__heading-row {
        flex-direction: column;
        align-items: flex-start;
    }

    .workspace-content__actions {
        width: 100%;
    }

    .workspace-content__actions .secondary-button,
    .workspace-content__actions .submit-button {
        width: 100%;
    }
}
</style>
