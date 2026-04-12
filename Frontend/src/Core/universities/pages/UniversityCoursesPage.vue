<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const isLoading = ref(false);
const isJoining = ref(false);
const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));

const joinLabel = computed(() => isTeacher.value ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const plannedCourseBlocks = computed(() => {
    return [
        {
            title: 'Course feed area',
            description: 'This center section is prepared for the Courses service list endpoint with pagination and role-aware actions.',
            status: 'Planned',
        },
        {
            title: 'Teacher actions panel',
            description: 'Teachers will be able to create and edit courses directly from this page once Courses UI is connected.',
            status: isTeacher.value ? 'Ready for teacher flow' : 'Hidden for students',
        },
        {
            title: 'Student learning queue',
            description: 'Students will see ordered lessons and progress entry points after Courses lesson views are integrated.',
            status: 'Planned',
        },
    ];
});

function normalizeValue(payload) {
    return payload.value || payload.Value || null;
}

function normalizeItems(payload) {
    return payload.items || payload.Items || [];
}

async function loadUniversity() {
    isLoading.value = true;
    actionMessage.value = '';
    errorMessage.value = '';

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
        isLoading.value = false;
    }
}

async function joinUniversity() {
    if (!university.value?.isOpened || isMember.value) {
        return;
    }

    isJoining.value = true;
    actionMessage.value = '';
    errorMessage.value = '';

    try {
        const response = await universityApi.enterUniversity(universityName.value);
        const serverMessage = response?.message || response?.Message;
        actionMessage.value = serverMessage || 'You entered the university as student.';
        isMember.value = true;
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

onMounted(loadUniversity);
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="courses"
            subtitle="Main study space prepared for university courses and lesson flow."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoading"
            @join="joinUniversity"
        />

        <section class="surface-card workspace-content">
            <div class="workspace-content__heading-row">
                <div>
                    <p class="section-kicker">University courses</p>
                    <h2 class="section-title">Learning center</h2>
                </div>

                <button class="secondary-button" type="button" @click="loadUniversity" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh workspace' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <div class="hero-placeholder surface-card">
                <AppIcon name="courses" />
                <div>
                    <h3>Courses integration is the primary center block</h3>
                    <p>
                        This page is ready for Courses service UI wiring. The center area will host university courses, while keeping the same dashboard language and spacing.
                    </p>
                </div>
            </div>

            <div class="plan-grid">
                <article v-for="block in plannedCourseBlocks" :key="block.title" class="surface-card plan-card hover-lift">
                    <p class="section-kicker">{{ block.status }}</p>
                    <h3>{{ block.title }}</h3>
                    <p class="section-copy">{{ block.description }}</p>
                </article>
            </div>

            <footer class="next-steps">
                <button class="secondary-button" type="button" @click="router.push({ name: 'UniversityInfo', params: { name: universityName } })">
                    Open university info
                </button>
                <button class="secondary-button" type="button" @click="router.push({ name: 'UniversityForums', params: { name: universityName } })">
                    Open university forums
                </button>
            </footer>
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
    align-items: center;
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

.plan-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.85rem;
}

.plan-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.plan-card h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.next-steps {
    display: flex;
    justify-content: flex-end;
    gap: 0.7rem;
    flex-wrap: wrap;
}

@media (max-width: 900px) {
    .plan-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 650px) {
    .workspace-content__heading-row {
        flex-direction: column;
        align-items: flex-start;
    }

    .next-steps {
        width: 100%;
    }

    .next-steps .secondary-button {
        width: 100%;
    }
}
</style>
