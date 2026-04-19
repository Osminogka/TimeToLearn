<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import universityApi from '../services/universityApi';

const universities = ref([]);
const isLoading = ref(false);
const errorMessage = ref('');
const infoMessage = ref('');
const page = ref(1);
const pageSize = ref(8);
const totalCount = ref(0);
const router = useRouter();

const hasItems = computed(() => universities.value.length > 0);
const hasNextPage = computed(() => page.value * pageSize.value < totalCount.value);
const hasPrevPage = computed(() => page.value > 1);

function normalizeItems(payload) {
    return payload.items || payload.Items || [];
}

function normalizeTotalCount(payload) {
    return payload.totalCount || payload.TotalCount || 0;
}

function openUniversitySpace(item) {
    const name = item.name || item.Name || '';
    if (!name) {
        return;
    }

    router.push({ name: 'UniversityCourses', params: { name } });
}

async function loadMyUniversities() {
    isLoading.value = true;
    errorMessage.value = '';
    infoMessage.value = '';

    try {
        const result = await universityApi.getMyUniversities({
            page: page.value,
            pageSize: pageSize.value,
        });

        universities.value = normalizeItems(result);
        totalCount.value = normalizeTotalCount(result);

        if (!universities.value.length) {
            infoMessage.value = 'You are not a member of any university yet.';
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load your universities.';
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
    loadMyUniversities();
}

function goToPrevPage() {
    if (!hasPrevPage.value) {
        return;
    }

    page.value -= 1;
    loadMyUniversities();
}

onMounted(loadMyUniversities);
</script>

<template>
    <main class="page-shell">
        <section class="content-panel surface-card">
            <div class="content-panel__top">
                <div>
                    <p class="section-kicker">Membership</p>
                    <h2 class="section-title">My universities</h2>
                </div>

                <button class="secondary-button" @click="loadMyUniversities" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="infoMessage" class="state-message">{{ infoMessage }}</p>

            <div v-if="hasItems" class="university-grid">
                <article
                    v-for="item in universities"
                    :key="item.name"
                    class="university-card surface-card hover-lift university-card--clickable"
                    role="button"
                    tabindex="0"
                    @click="openUniversitySpace(item)"
                    @keydown.enter.prevent="openUniversitySpace(item)"
                    @keydown.space.prevent="openUniversitySpace(item)"
                >
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

.university-card--clickable {
    cursor: pointer;
}

.university-card--clickable:focus-visible {
    outline: none;
    border-color: rgba(143, 44, 226, 0.34);
    box-shadow: 0 0 0 4px rgba(143, 44, 226, 0.14);
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
