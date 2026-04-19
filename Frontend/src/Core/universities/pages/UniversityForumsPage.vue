<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';
import forumApi from '@/Forums/services/forumApi';
import ForumTopicCard from '@/Forums/components/ForumTopicCard.vue';

const route = useRoute();
const TOPICS_PAGE_SIZE = 10;

const university = ref(null);
const topics = ref([]);
const isLoadingWorkspace = ref(false);
const isLoadingTopics = ref(false);
const isJoining = ref(false);
const isCreatingTopic = ref(false);
const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');
const topicPage = ref(0);
const hasMoreTopics = ref(false);

const createTopicForm = reactive({
    title: '',
    content: '',
});

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));
const hasTopics = computed(() => topics.value.length > 0);
const canLoadPrevTopics = computed(() => topicPage.value > 0);
const canViewForums = computed(() => isMember.value);

const joinLabel = computed(() => isTeacher.value ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

function normalizeValue(payload) {
    return payload?.value || payload?.Value || null;
}

function normalizeItems(payload) {
    return payload?.items || payload?.Items || payload?.values || payload?.Values || [];
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

async function loadUniversity() {
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

        isMember.value = myItems.some((item) => {
            const currentName = String(item.name || item.Name || '').toLowerCase();
            return currentName === universityName.value.toLowerCase();
        });
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load university forums.';
        university.value = null;
        isMember.value = false;
    } finally {
        isLoadingWorkspace.value = false;
    }
}

async function loadTopics({ resetPage = false } = {}) {
    if (!canViewForums.value) {
        topics.value = [];
        hasMoreTopics.value = false;
        return;
    }

    if (resetPage) {
        topicPage.value = 0;
    }

    isLoadingTopics.value = true;

    try {
        const response = await forumApi.getUniversityTopics(universityName.value, {
            page: topicPage.value,
        });

        const values = normalizeItems(response);
        topics.value = values;
        hasMoreTopics.value = values.length === TOPICS_PAGE_SIZE;
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load forum topics.';
        topics.value = [];
        hasMoreTopics.value = false;
    } finally {
        isLoadingTopics.value = false;
    }
}

async function refreshWorkspace() {
    await loadUniversity();
    if (canViewForums.value) {
        await loadTopics({ resetPage: true });
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
        await loadTopics({ resetPage: true });
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

async function createTopic() {
    if (!createTopicForm.title.trim() || !createTopicForm.content.trim() || isCreatingTopic.value) {
        return;
    }

    isCreatingTopic.value = true;
    clearMessages();

    try {
        const response = await forumApi.createTopic({
            topicTitle: createTopicForm.title.trim(),
            topicContent: createTopicForm.content.trim(),
            universityName: universityName.value,
        });

        actionMessage.value = readMessage(response) || 'Topic created successfully.';
        createTopicForm.title = '';
        createTopicForm.content = '';
        await loadTopics({ resetPage: true });
    } catch (error) {
        errorMessage.value = error?.message || 'Could not create topic.';
    } finally {
        isCreatingTopic.value = false;
    }
}

async function goToPrevTopicPage() {
    if (!canLoadPrevTopics.value || isLoadingTopics.value) {
        return;
    }

    topicPage.value -= 1;
    await loadTopics();
}

async function goToNextTopicPage() {
    if (!hasMoreTopics.value || isLoadingTopics.value) {
        return;
    }

    topicPage.value += 1;
    await loadTopics();
}

onMounted(async () => {
    await refreshWorkspace();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="forums"
            subtitle="Forum collaboration space."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card forum-content">
            <div class="forum-content__heading-row">
                <div>
                    <p class="section-kicker">University forums</p>
                    <h2 class="section-title">Community board</h2>
                </div>

                <button class="secondary-button" type="button" @click="refreshWorkspace" :disabled="isLoadingWorkspace || isLoadingTopics">
                    {{ isLoadingWorkspace || isLoadingTopics ? 'Refreshing...' : 'Refresh workspace' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="forum-hero surface-card">
                <AppIcon name="forum" />
                <div>
                    <h3>{{ canViewForums ? 'University discussion feed' : 'Join this university to write and read posts' }}</h3>
                    <p class="section-copy">
                        {{
                            canViewForums
                                ? 'Any member can create text posts, comment, and reply in nested discussion threads.'
                                : 'Access to forum content is available to university members only.'
                        }}
                    </p>
                </div>
            </article>

            <form v-if="canViewForums" class="surface-card topic-form" @submit.prevent="createTopic">
                <h3>Create a post</h3>
                <input
                    v-model="createTopicForm.title"
                    class="input-field"
                    type="text"
                    maxlength="30"
                    placeholder="Post title"
                    required
                />
                <textarea
                    v-model="createTopicForm.content"
                    class="input-field topic-form__textarea"
                    maxlength="1000"
                    rows="5"
                    placeholder="Write your post text"
                    required
                />
                <button class="submit-button" type="submit" :disabled="isCreatingTopic || isLoadingTopics">
                    {{ isCreatingTopic ? 'Publishing...' : 'Publish post' }}
                </button>
            </form>

            <div v-if="canViewForums" class="topics-grid">
                <ForumTopicCard
                    v-for="topic in topics"
                    :key="topic.id || topic.Id"
                    :topic="topic"
                    :university-name="universityName"
                />

                <p v-if="!isLoadingTopics && !hasTopics" class="state-message">
                    No posts yet. Create the first one for this university.
                </p>

                <footer class="pagination-row">
                    <button class="secondary-button" type="button" @click="goToPrevTopicPage" :disabled="!canLoadPrevTopics || isLoadingTopics">
                        Previous posts
                    </button>
                    <p class="pagination-row__status">Page {{ topicPage + 1 }}</p>
                    <button class="secondary-button" type="button" @click="goToNextTopicPage" :disabled="!hasMoreTopics || isLoadingTopics">
                        Next posts
                    </button>
                </footer>
            </div>

            <p v-else class="state-message">Membership is required to access this university forum.</p>
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
    align-items: flex-start;
    gap: 0.8rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.06));
}

.forum-hero h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.forum-hero p {
    margin: 0.35rem 0 0;
    color: var(--ttl-text-secondary);
    line-height: 1.6;
}

.topic-form {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.topic-form h3 {
    margin: 0;
    color: var(--ttl-text-primary);
}

.topic-form__textarea {
    resize: vertical;
}

.topics-grid {
    display: flex;
    flex-direction: column;
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

@media (max-width: 640px) {
    .forum-content__heading-row {
        flex-direction: column;
        align-items: flex-start;
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
