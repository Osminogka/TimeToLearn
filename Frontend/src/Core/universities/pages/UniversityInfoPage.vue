<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const route = useRoute();

const university = ref(null);
const students = ref([]);
const teachers = ref([]);
const isLoading = ref(false);
const isLoadingMembers = ref(false);
const isJoining = ref(false);
const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));

const joinLabel = computed(() => isTeacher.value ? 'Request as teacher' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

function normalizeValue(payload) {
    return payload.value || payload.Value || null;
}

function normalizeItems(payload) {
    return payload.items || payload.Items || [];
}

async function loadUniversity() {
    isLoading.value = true;
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
        errorMessage.value = error?.message || 'Failed to load university information.';
        university.value = null;
        isMember.value = false;
    } finally {
        isLoading.value = false;
    }
}

async function loadMembers() {
    if (!universityName.value) {
        return;
    }

    isLoadingMembers.value = true;

    try {
        const [teachersResponse, studentsResponse] = await Promise.all([
            universityApi.getUniversityTeachers(universityName.value, { page: 1, pageSize: 8 }),
            universityApi.getUniversityStudents(universityName.value, { page: 1, pageSize: 8 }),
        ]);

        teachers.value = normalizeItems(teachersResponse);
        students.value = normalizeItems(studentsResponse);
    } catch {
        teachers.value = [];
        students.value = [];
    } finally {
        isLoadingMembers.value = false;
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
        const response = isTeacher.value
            ? await universityApi.requestJoinAsTeacher(universityName.value)
            : await universityApi.enterUniversity(universityName.value);

        const serverMessage = response?.message || response?.Message;
        actionMessage.value = serverMessage || (isTeacher.value ? 'Join request sent to university director.' : 'You entered the university successfully.');

        if (!isTeacher.value) {
            isMember.value = true;
            await loadMembers();
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

onMounted(async () => {
    await loadUniversity();
    await loadMembers();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="info"
            subtitle="Overview of university details, access setup, and members."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoading"
            @join="joinUniversity"
        />

        <section class="surface-card page-content">
            <div class="page-content__heading-row">
                <div>
                    <p class="section-kicker">University profile</p>
                    <h2 class="section-title">Information</h2>
                </div>

                <button class="secondary-button" type="button" @click="loadUniversity" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh info' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="surface-card info-card">
                <div class="info-card__top">
                    <h3>University details</h3>
                    <span class="pill" :class="university?.isOpened ? 'pill--accent' : 'pill--pink'">
                        {{ university?.isOpened ? 'Open access' : 'Private access' }}
                    </span>
                </div>

                <p class="section-copy">{{ university?.description || 'Description will appear here.' }}</p>

                <div class="address-line">
                    <AppIcon name="info" />
                    <p>
                        {{ university?.address?.country || 'Country N/A' }}
                        •
                        {{ university?.address?.city || 'City N/A' }}
                        •
                        {{ university?.address?.street || 'Street N/A' }}
                    </p>
                </div>
            </article>

            <div class="members-grid">
                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Teachers</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : teachers.length }}</span>
                    </div>

                    <p v-if="!teachers.length" class="member-empty">No teachers available or access is limited.</p>

                    <ul v-else class="member-list">
                        <li v-for="teacherName in teachers" :key="teacherName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ teacherName }}</span>
                        </li>
                    </ul>
                </article>

                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Students</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : students.length }}</span>
                    </div>

                    <p v-if="!students.length" class="member-empty">No students available or access is limited.</p>

                    <ul v-else class="member-list">
                        <li v-for="studentName in students" :key="studentName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ studentName }}</span>
                        </li>
                    </ul>
                </article>
            </div>
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

.page-content {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.page-content__heading-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
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

.info-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.info-card__top {
    display: flex;
    justify-content: space-between;
    gap: 0.7rem;
    align-items: center;
}

.info-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.address-line {
    display: flex;
    align-items: center;
    gap: 0.65rem;
}

.address-line p {
    margin: 0;
    color: var(--ttl-text-muted);
}

.members-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.85rem;
}

.member-block {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.member-block__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.member-block__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.member-empty {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.member-list {
    margin: 0;
    padding: 0;
    list-style: none;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.member-item {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    padding: 0.5rem 0.6rem;
    border-radius: 0.8rem;
    background: rgba(143, 44, 226, 0.06);
    color: var(--ttl-text-primary);
}

@media (max-width: 900px) {
    .members-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .page-content__heading-row,
    .info-card__top {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
