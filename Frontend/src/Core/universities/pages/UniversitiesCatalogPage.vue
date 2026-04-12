<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import UniversitiesWorkspaceHeader from '../components/UniversitiesWorkspaceHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';

const universities = ref([]);
const isLoading = ref(false);
const actionLoadingMap = ref({});
const errorMessage = ref('');
const infoMessage = ref('');
const page = ref(1);
const pageSize = ref(8);
const totalCount = ref(0);
const router = useRouter();

const hasItems = computed(() => universities.value.length > 0);
const hasNextPage = computed(() => page.value * pageSize.value < totalCount.value);
const hasPrevPage = computed(() => page.value > 1);
const isTeacher = computed(() => isTeacherRole(user.value.role));

function normalizeItems(payload) {
    return payload.items || payload.Items || [];
}

function normalizeTotalCount(payload) {
    return payload.totalCount || payload.TotalCount || 0;
}

function getUniversityName(item) {
    return item.name || item.Name || '';
}

function isOpenedUniversity(item) {
    return Boolean(item.isOpened ?? item.IsOpened);
}

function getActionKey(item) {
    return getUniversityName(item).toLowerCase();
}

function getJoinLabel(item) {
    if (!isOpenedUniversity(item)) {
        return 'Private access';
    }

    return isTeacher.value ? 'Enter as student' : 'Enter university';
}

function canJoin(item) {
    return isOpenedUniversity(item);
}

function openUniversitySpace(item) {
    const name = getUniversityName(item);
    if (!name) {
        return;
    }

    router.push({ name: 'UniversityCourses', params: { name } });
}

async function joinUniversity(item) {
    if (!canJoin(item)) {
        return;
    }

    const name = getUniversityName(item);
    if (!name) {
        return;
    }

    const key = getActionKey(item);
    actionLoadingMap.value = { ...actionLoadingMap.value, [key]: true };
    errorMessage.value = '';
    infoMessage.value = '';

    try {
        const response = await universityApi.enterUniversity(name);
        infoMessage.value = response?.message || response?.Message || 'You entered the university as student.';
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        actionLoadingMap.value = { ...actionLoadingMap.value, [key]: false };
    }
}

async function loadUniversities() {
    isLoading.value = true;
    errorMessage.value = '';
    infoMessage.value = '';

    try {
        const result = await universityApi.getAvailableUniversities({
            page: page.value,
            pageSize: pageSize.value,
        });

        universities.value = normalizeItems(result);
        totalCount.value = normalizeTotalCount(result);

        if (!universities.value.length) {
            infoMessage.value = 'No available universities right now. You are already a member of everything in this list.';
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load universities.';
        universities.value = [];
        totalCount.value = 0;
    } finally {
        isLoading.value = false;
    }
}

function goToNextPage() {
    if (!hasNextPage.value) {
        return;
    }

    page.value += 1;
    loadUniversities();
}

function goToPrevPage() {
    if (!hasPrevPage.value) {
        return;
    }

    page.value -= 1;
    loadUniversities();
}

onMounted(() => {
    authUtils.getCurrentUser();
    loadUniversities();
});
</script>

<template>
    <main class="page-shell">
        <UniversitiesWorkspaceHeader
            active="all"
            title="Explore universities"
            subtitle="Browse every university and quickly find communities that match your learning goals."
        />

        <section class="content-panel surface-card">
            <div class="content-panel__top">
                <div>
                    <p class="section-kicker">Catalog</p>
                    <h2 class="section-title">All universities</h2>
                </div>

                <button class="secondary-button" @click="loadUniversities" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="infoMessage" class="state-message">{{ infoMessage }}</p>

            <div v-if="hasItems" class="university-grid">
                <article v-for="item in universities" :key="item.name" class="university-card surface-card hover-lift">
                    <div class="university-card__top">
                        <h3>{{ item.name }}</h3>
                        <span class="pill" :class="item.isOpened ? 'pill--accent' : 'pill--pink'">
                            {{ item.isOpened ? 'Open' : 'Private' }}
                        </span>
                    </div>

                    <p class="section-copy">{{ item.description }}</p>

                    <p class="university-card__address">
                        {{ item.address?.country || 'Country N/A' }}
                        •
                        {{ item.address?.city || 'City N/A' }}
                        •
                        {{ item.address?.street || 'Street N/A' }}
                    </p>

                    <div class="university-card__actions">
                        <button class="secondary-button" type="button" @click="openUniversitySpace(item)">
                            Open university
                        </button>

                        <button
                            class="submit-button"
                            type="button"
                            @click="joinUniversity(item)"
                            :disabled="!canJoin(item) || isLoading || actionLoadingMap[getActionKey(item)]"
                        >
                            {{ actionLoadingMap[getActionKey(item)] ? 'Please wait...' : getJoinLabel(item) }}
                        </button>
                    </div>
                </article>
            </div>

            <footer class="pagination-row">
                <button class="secondary-button" @click="goToPrevPage" :disabled="!hasPrevPage || isLoading">Previous</button>
                <p class="pagination-row__status">Page {{ page }} · {{ totalCount }} total</p>
                <button class="secondary-button" @click="goToNextPage" :disabled="!hasNextPage || isLoading">Next</button>
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

.content-panel {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.content-panel__top {
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

.university-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.9rem;
}

.university-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.university-card__top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.7rem;
}

.university-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 1.1rem;
    letter-spacing: -0.02em;
}

.university-card__address {
    margin: 0;
    color: var(--ttl-text-muted);
    font-size: 0.88rem;
    line-height: 1.5;
}

.university-card__actions {
    margin-top: 0.25rem;
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.65rem;
}

.university-card__actions .secondary-button,
.university-card__actions .submit-button {
    min-height: 2.65rem;
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
    .university-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .university-card__actions {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 560px) {
    .pagination-row {
        flex-direction: column;
        align-items: stretch;
    }

    .pagination-row .secondary-button {
        width: 100%;
    }

    .content-panel__top {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
