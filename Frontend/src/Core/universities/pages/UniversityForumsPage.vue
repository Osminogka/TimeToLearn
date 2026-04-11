<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const route = useRoute();

const university = ref(null);
const isLoading = ref(false);
const isJoining = ref(false);
const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));

const joinLabel = computed(() => isTeacher.value ? 'Request as teacher' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const forumModules = [
    {
        title: 'Topic feed',
        description: 'Upcoming Forums integration will render paged topics for this university in this section.',
        status: 'Planned',
    },
    {
        title: 'Comment threads',
        description: 'Nested comment and reply interactions will be connected to the Forums comments endpoint.',
        status: 'Planned',
    },
    {
        title: 'Like/dislike actions',
        description: 'Role-aware interaction controls and lightweight counters will be placed in each post card.',
        status: 'Planned',
    },
];

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
        errorMessage.value = error?.message || 'Failed to load university forums workspace.';
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
        const response = isTeacher.value
            ? await universityApi.requestJoinAsTeacher(universityName.value)
            : await universityApi.enterUniversity(universityName.value);

        const serverMessage = response?.message || response?.Message;
        actionMessage.value = serverMessage || (isTeacher.value ? 'Join request sent to university director.' : 'You entered the university successfully.');

        if (!isTeacher.value) {
            isMember.value = true;
        }
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
            active-tab="forums"
            subtitle="Forum collaboration area prepared for topic and comment functionality."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoading"
            @join="joinUniversity"
        />

        <section class="surface-card forum-content">
            <div class="forum-content__heading-row">
                <div>
                    <p class="section-kicker">University forums</p>
                    <h2 class="section-title">Discussion space</h2>
                </div>

                <button class="secondary-button" type="button" @click="loadUniversity" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh workspace' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="forum-hero surface-card">
                <AppIcon name="forum" />
                <div>
                    <h3>Forums feature shell is ready</h3>
                    <p>
                        This page is intentionally prepared for the Forums service without adding temporary patterns. Topic cards and comments will plug in here next.
                    </p>
                </div>
            </article>

            <div class="module-grid">
                <article v-for="module in forumModules" :key="module.title" class="surface-card module-card hover-lift">
                    <p class="section-kicker">{{ module.status }}</p>
                    <h3>{{ module.title }}</h3>
                    <p class="section-copy">{{ module.description }}</p>
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

.forum-content {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.forum-content__heading-row {
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

.forum-hero {
    padding: 1rem;
    display: flex;
    align-items: center;
    gap: 0.8rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.06));
}

.forum-hero h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.forum-hero p {
    margin: 0.3rem 0 0;
    color: var(--ttl-text-secondary);
    line-height: 1.6;
}

.module-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.85rem;
}

.module-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.module-card h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

@media (max-width: 900px) {
    .module-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .forum-content__heading-row {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
