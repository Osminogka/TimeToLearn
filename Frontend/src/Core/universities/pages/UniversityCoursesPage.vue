<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';
import courseApi from '@/Courses/services/courseApi';
import CourseCard from '@/Courses/components/CourseCard.vue';
import CourseFormPanel from '@/Courses/components/CourseFormPanel.vue';

const COURSES_PAGE_SIZE = 10;

const route = useRoute();
const router = useRouter();

const university = ref(null);
const courses = ref([]);
const isLoadingWorkspace = ref(false);
const isLoadingCourses = ref(false);
const isJoining = ref(false);
const isSubmittingCourse = ref(false);
const isDeletingCourse = ref(false);
const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');
const formErrorMessage = ref('');
const coursePage = ref(1);
const hasNextCoursePage = ref(false);
const isCourseFormOpen = ref(false);
const editingCourseId = ref(0);
const courseProgressMap = ref({});

const universityName = computed(() => String(route.params.name || ''));
const canManageCourses = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const editingCourse = computed(() => {
    if (!editingCourseId.value) {
        return null;
    }

    return courses.value.find((course) => Number(course.id || course.Id) === editingCourseId.value) || null;
});
const hasCourses = computed(() => courses.value.length > 0);
const hasPrevCoursePage = computed(() => coursePage.value > 1);
const canCreateCourses = computed(() => canManageCourses.value);
const canViewCourses = computed(() => isMember.value);

const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);
const isEditingMode = computed(() => Boolean(editingCourseId.value));
const formInitialTitle = computed(() => editingCourse.value?.title || editingCourse.value?.Title || '');
const formInitialDescription = computed(() => editingCourse.value?.description || editingCourse.value?.Description || '');

function normalizeValue(payload) {
    return payload?.value || payload?.Value || null;
}

function normalizeItems(payload) {
    return payload?.items || payload?.Items || payload?.values || payload?.Values || [];
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function clearStateMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

function resetCourseForm() {
    formErrorMessage.value = '';
    editingCourseId.value = 0;
    isCourseFormOpen.value = false;
}

function getCourseId(course) {
    return Number(course?.id || course?.Id || 0);
}

function normalizeProgress(payload) {
    return payload?.value || payload?.Value || null;
}

async function loadCourseProgressForStudents() {
    if (!canViewCourses.value || canManageCourses.value || !courses.value.length) {
        courseProgressMap.value = {};
        return;
    }

    const entries = await Promise.all(
        courses.value.map(async (course) => {
            const id = getCourseId(course);
            if (!id) {
                return null;
            }

            try {
                const response = await courseApi.getCourseProgress(id);
                return [id, normalizeProgress(response)];
            } catch {
                return [id, null];
            }
        })
    );

    courseProgressMap.value = Object.fromEntries(entries.filter(Boolean));
}

async function loadUniversity() {
    isLoadingWorkspace.value = true;
    clearStateMessages();

    try {
        authUtils.getCurrentUser();

        const [universityResponse, myUniversitiesResponse] = await Promise.all([
            universityApi.getUniversityByName(universityName.value),
            universityApi.getMyUniversities({ page: 1, pageSize: 100 }),
        ]);

        university.value = normalizeValue(universityResponse);
        const myItems = normalizeItems(myUniversitiesResponse);

        isMember.value = myItems.some((item) => {
            const currentName = String(item.name || item.Name || '').toLowerCase();
            return currentName === universityName.value.toLowerCase();
        });
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load university workspace.';
        university.value = null;
        isMember.value = false;
    } finally {
        isLoadingWorkspace.value = false;
    }
}

async function loadCourses({ resetPage = false } = {}) {
    if (!canViewCourses.value) {
        courses.value = [];
        hasNextCoursePage.value = false;
        return;
    }

    if (resetPage) {
        coursePage.value = 1;
    }

    isLoadingCourses.value = true;
    formErrorMessage.value = '';

    try {
        const response = await courseApi.getUniversityCourses(universityName.value, {
            page: coursePage.value - 1,
        });

        const values = normalizeItems(response);
        courses.value = values;
        hasNextCoursePage.value = values.length === COURSES_PAGE_SIZE;
        await loadCourseProgressForStudents();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load courses.';
        courses.value = [];
        hasNextCoursePage.value = false;
        courseProgressMap.value = {};
    } finally {
        isLoadingCourses.value = false;
    }
}

async function refreshWorkspace() {
    await loadUniversity();

    if (canViewCourses.value) {
        await loadCourses({ resetPage: true });
    }
}

async function joinUniversity() {
    if (!university.value?.isOpened || isMember.value) {
        return;
    }

    isJoining.value = true;
    clearStateMessages();

    try {
        const response = await universityApi.enterUniversity(universityName.value);
        actionMessage.value = readMessage(response) || 'You entered the university as student.';
        isMember.value = true;
        await loadCourses({ resetPage: true });
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

function openCreateCourseForm() {
    editingCourseId.value = 0;
    formErrorMessage.value = '';
    isCourseFormOpen.value = true;
}

function openEditCourseForm(course) {
    const courseId = Number(course?.id || course?.Id || 0);
    if (!courseId) {
        return;
    }

    editingCourseId.value = courseId;
    formErrorMessage.value = '';
    isCourseFormOpen.value = true;
}

function closeCourseForm() {
    resetCourseForm();
}

async function submitCourseForm(payload) {
    formErrorMessage.value = payload.validationError || '';
    if (payload.validationError) {
        return;
    }

    clearStateMessages();
    isSubmittingCourse.value = true;

    try {
        if (isEditingMode.value) {
            const courseId = editingCourseId.value;
            const response = await courseApi.updateCourse({
                courseId,
                title: payload.title,
                description: payload.description,
            });

            actionMessage.value = readMessage(response) || 'Course updated successfully.';
            await loadCourses();
        } else {
            const response = await courseApi.createCourse({
                title: payload.title,
                description: payload.description,
                universityName: universityName.value,
            });

            actionMessage.value = readMessage(response) || 'Course created successfully.';
            await loadCourses({ resetPage: true });
        }

        resetCourseForm();
    } catch (error) {
        formErrorMessage.value = error?.message || 'Could not save course.';
    } finally {
        isSubmittingCourse.value = false;
    }
}

function openLessonsWorkspace(course) {
    const courseId = Number(course?.id || course?.Id || 0);
    if (!courseId) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessons',
        params: {
            name: universityName.value,
            courseId,
        },
    });
}

async function removeCourse(course) {
    const courseId = Number(course?.id || course?.Id || 0);
    if (!courseId || !canManageCourses.value || isDeletingCourse.value) {
        return;
    }

    clearStateMessages();
    isDeletingCourse.value = true;

    try {
        const response = await courseApi.deleteCourse(courseId);
        actionMessage.value = readMessage(response) || 'Course deleted successfully.';

        if (isEditingMode.value && editingCourseId.value === courseId) {
            resetCourseForm();
        }

        await loadCourses();

        if (!courses.value.length && hasPrevCoursePage.value) {
            coursePage.value -= 1;
            await loadCourses();
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not delete course.';
    } finally {
        isDeletingCourse.value = false;
    }
}

async function goToPrevCoursePage() {
    if (!hasPrevCoursePage.value || isLoadingCourses.value) {
        return;
    }

    coursePage.value -= 1;
    await loadCourses();
}

async function goToNextCoursePage() {
    if (!hasNextCoursePage.value || isLoadingCourses.value) {
        return;
    }

    coursePage.value += 1;
    await loadCourses();
}

onMounted(async () => {
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
                    <p class="section-kicker">University courses</p>
                    <h2 class="section-title">Learning center</h2>
                </div>

                <div class="workspace-content__actions">
                    <button class="secondary-button" type="button" @click="refreshWorkspace" :disabled="isLoadingWorkspace || isLoadingCourses">
                        {{ isLoadingWorkspace || isLoadingCourses ? 'Refreshing...' : 'Refresh workspace' }}
                    </button>

                    <button
                        v-if="canCreateCourses"
                        class="submit-button"
                        type="button"
                        @click="openCreateCourseForm"
                        :disabled="isSubmittingCourse || isLoadingCourses"
                    >
                        Create course
                    </button>
                </div>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="hero-placeholder surface-card">
                <AppIcon name="courses" />
                <div>
                    <h3>{{ canViewCourses ? 'University course catalog' : 'Join to access courses' }}</h3>
                    <p class="section-copy">
                        {{
                            canViewCourses
                                ? 'Browse active courses, check lesson counts, and open the lesson workspace for each course.'
                                : 'This university allows members to access courses. Use the join action above to enter as a student.'
                        }}
                    </p>
                </div>
            </article>

            <CourseFormPanel
                v-if="isCourseFormOpen"
                :mode="isEditingMode ? 'edit' : 'create'"
                :initial-title="formInitialTitle"
                :initial-description="formInitialDescription"
                :is-submitting="isSubmittingCourse"
                :error-message="formErrorMessage"
                @submit="submitCourseForm"
                @cancel="closeCourseForm"
            />

            <div v-if="canViewCourses" class="courses-grid">
                <transition-group name="fade-rise" tag="div" class="courses-grid__items">
                    <CourseCard
                        v-for="course in courses"
                        :key="course.id || course.Id"
                        :course="course"
                        :progress="courseProgressMap[Number(course.id || course.Id || 0)] || null"
                        :can-edit="canManageCourses"
                        :can-delete="canManageCourses"
                        :disable-actions="isLoadingCourses || isSubmittingCourse || isDeletingCourse"
                        @edit="openEditCourseForm"
                        @delete="removeCourse"
                        @open-lessons="openLessonsWorkspace"
                    />
                </transition-group>

                <p v-if="!isLoadingCourses && !hasCourses" class="state-message">
                    No courses yet. {{ canCreateCourses ? 'Create the first course for this university.' : 'A teacher will publish courses soon.' }}
                </p>

                <footer class="pagination-row">
                    <button class="secondary-button" type="button" @click="goToPrevCoursePage" :disabled="!hasPrevCoursePage || isLoadingCourses">
                        Previous
                    </button>
                    <p class="pagination-row__status">Page {{ coursePage }}</p>
                    <button class="secondary-button" type="button" @click="goToNextCoursePage" :disabled="!hasNextCoursePage || isLoadingCourses">
                        Next
                    </button>
                </footer>
            </div>

            <p v-else class="state-message">Membership is required to view this university course list.</p>
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

.courses-grid {
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}

.courses-grid__items {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.85rem;
}

.pagination-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.8rem;
}

.pagination-row__status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.9rem;
}

@media (max-width: 900px) {
    .courses-grid__items {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 650px) {
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

    .pagination-row {
        flex-direction: column;
        align-items: stretch;
    }

    .pagination-row .secondary-button {
        width: 100%;
    }
}
</style>
