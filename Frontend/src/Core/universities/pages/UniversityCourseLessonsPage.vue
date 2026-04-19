<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import courseApi from '@/Courses/services/courseApi';
import LessonListMobileItem from '@/Courses/components/LessonListMobileItem.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const course = ref(null);
const lessons = ref([]);
const courseProgress = ref(null);

const isLoadingWorkspace = ref(false);
const isLoadingLessons = ref(false);
const isJoining = ref(false);
const isReordering = ref(false);

const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));

const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const canView = computed(() => isMember.value);
const hasLessons = computed(() => lessons.value.length > 0);

const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const progressCompletedCount = computed(() => Number(courseProgress.value?.completedLessons || courseProgress.value?.CompletedLessons || 0));
const progressTotalCount = computed(() => Number(courseProgress.value?.totalLessons || courseProgress.value?.TotalLessons || lessons.value.length));
const progressPercent = computed(() => {
    if (!progressTotalCount.value) {
        return 0;
    }

    const rawPercent = Number(courseProgress.value?.percentComplete || courseProgress.value?.PercentComplete || 0);
    if (Number.isFinite(rawPercent)) {
        return Math.max(0, Math.min(100, rawPercent));
    }

    return Math.round((progressCompletedCount.value / progressTotalCount.value) * 100);
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

function getCourseTitle() {
    return course.value?.title || course.value?.Title || 'Course lessons';
}

function getLessonId(lesson) {
    return Number(lesson?.id || lesson?.Id || 0);
}

function getOrderNumber(lesson) {
    return Number(lesson?.orderNumber || lesson?.OrderNumber || 0);
}

function sortLessons(rows) {
    return [...rows].sort((a, b) => {
        const orderDiff = getOrderNumber(a) - getOrderNumber(b);
        if (orderDiff !== 0) {
            return orderDiff;
        }

        return String(a?.title || a?.Title || '').localeCompare(String(b?.title || b?.Title || ''));
    });
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
        return;
    }

    isLoadingLessons.value = true;

    try {
        const response = await courseApi.getCourseLessons(courseId.value);
        lessons.value = sortLessons(normalizeItems(response));
    } catch (error) {
        lessons.value = [];
        errorMessage.value = error?.message || 'Failed to load lessons.';
    } finally {
        isLoadingLessons.value = false;
    }
}

async function loadCourseProgress() {
    if (!courseId.value || !canView.value || canManage.value) {
        courseProgress.value = null;
        return;
    }

    try {
        const response = await courseApi.getCourseProgress(courseId.value);
        courseProgress.value = normalizeValue(response);
    } catch {
        courseProgress.value = null;
    }
}

async function refreshWorkspace() {
    await loadUniversityMembership();

    if (canView.value) {
        await loadCourse();
        await loadLessons();
        await loadCourseProgress();
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
        await loadCourseProgress();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

function openCoursesWorkspace() {
    router.push({ name: 'UniversityCourses', params: { name: universityName.value } });
}

function openLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonDetail',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId,
        },
    });
}

function openEditLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonEdit',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId,
        },
    });
}

function openCreateLesson() {
    router.push({
        name: 'UniversityCourseLessonCreate',
        params: {
            name: universityName.value,
            courseId: courseId.value,
        },
    });
}

function createReorderPayload(rows) {
    return rows.map((item, index) => ({
        lessonId: getLessonId(item),
        orderNumber: index + 1,
    }));
}

function applyReorderedNumbers(rows) {
    return rows.map((item, index) => ({
        ...item,
        orderNumber: index + 1,
        OrderNumber: index + 1,
    }));
}

async function moveLesson(lesson, direction) {
    if (!canManage.value || isReordering.value) {
        return;
    }

    const currentId = getLessonId(lesson);
    if (!currentId) {
        return;
    }

    const currentIndex = lessons.value.findIndex((item) => getLessonId(item) === currentId);
    if (currentIndex < 0) {
        return;
    }

    const targetIndex = currentIndex + direction;
    if (targetIndex < 0 || targetIndex >= lessons.value.length) {
        return;
    }

    const reordered = [...lessons.value];
    const [moved] = reordered.splice(currentIndex, 1);
    reordered.splice(targetIndex, 0, moved);

    const normalized = applyReorderedNumbers(reordered);
    const payload = createReorderPayload(normalized);

    clearMessages();
    isReordering.value = true;

    try {
        await courseApi.reorderLessons({
            courseId: courseId.value,
            items: payload,
        });

        lessons.value = normalized;
        actionMessage.value = 'Lessons reordered.';
    } catch (error) {
        errorMessage.value = error?.message || 'Could not reorder lessons.';
        await loadLessons();
    } finally {
        isReordering.value = false;
    }
}

onMounted(async () => {
    authUtils.getCurrentUser();
    await refreshWorkspace();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="courses"
            subtitle="Course lessons"
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card workspace-card">
            <div class="workspace-card__head">
                <div>
                    <p class="section-kicker">Lessons</p>
                    <h2 class="section-title">{{ getCourseTitle() }}</h2>
                </div>

                <div class="workspace-card__actions">
                    <button class="secondary-button" type="button" @click="openCoursesWorkspace">Back</button>
                    <button class="secondary-button" type="button" :disabled="isLoadingWorkspace || isLoadingLessons" @click="refreshWorkspace">
                        {{ isLoadingWorkspace || isLoadingLessons ? 'Refreshing...' : 'Refresh' }}
                    </button>
                    <button v-if="canManage" class="submit-button" type="button" :disabled="isLoadingLessons" @click="openCreateLesson">
                        New lesson
                    </button>
                </div>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article v-if="canView && !canManage" class="surface-card progress-card">
                <div class="progress-card__head">
                    <p class="section-kicker">Progress</p>
                    <p class="progress-card__status">{{ progressCompletedCount }} / {{ progressTotalCount }} done</p>
                </div>

                <div class="progress-card__track" role="progressbar" :aria-valuenow="progressPercent" aria-valuemin="0" aria-valuemax="100">
                    <div class="progress-card__fill" :style="{ width: `${progressPercent}%` }" />
                </div>
            </article>

            <div v-if="canView" class="lessons-list">
                <LessonListMobileItem
                    v-for="(lesson, index) in lessons"
                    :key="getLessonId(lesson)"
                    :lesson="lesson"
                    :can-manage="canManage"
                    :is-busy="isLoadingLessons || isReordering"
                    :disable-move-up="index === 0"
                    :disable-move-down="index === lessons.length - 1"
                    @open="openLesson"
                    @edit="openEditLesson"
                    @move-up="() => moveLesson(lesson, -1)"
                    @move-down="() => moveLesson(lesson, 1)"
                />

                <p v-if="!isLoadingLessons && !hasLessons" class="state-message">No lessons yet.</p>
            </div>

            <p v-else class="state-message">Membership is required to view lessons in this course.</p>
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

.workspace-card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.workspace-card__head {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.workspace-card__actions {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.55rem;
}

.workspace-card__actions .submit-button,
.workspace-card__actions .secondary-button {
    min-height: 2.6rem;
}

.lessons-list {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.progress-card {
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.08));
}

.progress-card__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
}

.progress-card__status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
}

.progress-card__track {
    height: 0.62rem;
    border-radius: 0.6rem;
    background: rgba(143, 44, 226, 0.14);
    overflow: hidden;
}

.progress-card__fill {
    height: 100%;
    border-radius: 0.6rem;
    background: linear-gradient(90deg, rgba(143, 44, 226, 0.85), rgba(255, 95, 162, 0.85));
    transition: width var(--ttl-transition-base);
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

@media (max-width: 620px) {
    .workspace-card__actions {
        grid-template-columns: 1fr;
    }
}
</style>
