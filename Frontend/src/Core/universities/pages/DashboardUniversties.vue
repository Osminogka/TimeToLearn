<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AppIcon from '@/Shared/components/AppIcon.vue';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import universityApi from '../services/universityApi';

const router = useRouter();

const myUniversities = ref([]);
const isLoadingUniversities = ref(false);
const dashboardMessage = ref('');

const isTeacher = computed(() => isTeacherRole(user.value.role));
const roleState = computed(() => isTeacher.value ? 'Teacher' : 'Student');

const quickActions = computed(() => {
    if (isTeacher.value) {
        return [
            {
                icon: 'create',
                title: 'Create new university',
                copy: 'Launch a learning space with a name, description, and access settings.',
                routeName: 'UniversitiesCreate',
                button: 'Start creation',
            },
            {
                icon: 'users',
                title: 'Manage my universities',
                copy: 'Review the spaces you belong to and keep your community up to date.',
                routeName: 'UniversitiesMine',
                button: 'Open list',
            },
            {
                icon: 'account',
                title: 'Manage my profile',
                copy: 'Update your account details, role, and teacher verification in one place.',
                routeName: 'AccountManagement',
                button: 'Open profile',
            },
        ];
    }

    return [
        {
            icon: 'join',
            title: 'Join a university',
            copy: 'Browse the full catalog and enter a new community that fits your goals.',
            routeName: 'UniversitiesAll',
            button: 'Browse catalog',
        },
        {
            icon: 'users',
            title: 'My universities',
            copy: 'See your enrolled spaces and quickly return to where you are active.',
            routeName: 'UniversitiesMine',
            button: 'Open list',
        },
        {
            icon: 'account',
            title: 'Manage my profile',
            copy: 'Update account details and switch roles when needed.',
            routeName: 'AccountManagement',
            button: 'Open profile',
        },
    ];
});

const hasUniversities = computed(() => myUniversities.value.length > 0);
const displayedUniversities = computed(() => myUniversities.value.slice(0, 4));

function normalizeItems(payload) {
    return payload.items || payload.Items || [];
}

async function loadDashboardData() {
    dashboardMessage.value = '';
    isLoadingUniversities.value = true;

    try {
        authUtils.getCurrentUser();
        const universitiesResponse = await universityApi.getMyUniversities({ page: 1, pageSize: 4 });

        myUniversities.value = normalizeItems(universitiesResponse);
    } catch (error) {
        dashboardMessage.value = error?.message || 'Could not load dashboard data.';
        myUniversities.value = [];
    } finally {
        isLoadingUniversities.value = false;
    }
}

onMounted(loadDashboardData);
</script>

<template>
    <main class="dashboard-shell">
        <section class="dashboard-hero surface-card surface-card--raised">
            <div class="dashboard-hero__copy">
                <p class="section-kicker">Student dashboard</p>
                <h1 class="section-title">Hi, {{ user.name || 'Student' }}. Your day starts here.</h1>
                <p class="section-copy">
                    {{ isTeacher
                        ? 'Manage your learning communities, create new universities, and keep your profile in sync.'
                        : 'Check the universities you already belong to, then join a new one when you are ready.' }}
                </p>

                <div class="dashboard-hero__chips">
                    <span class="pill pill--accent"><AppIcon name="profile" />{{ user.email || 'Account active' }}</span>
                    <span class="pill"><AppIcon name="role" />{{ roleState }}</span>
                    <span class="pill pill--pink"><AppIcon name="users" />{{ hasUniversities ? `${myUniversities.length} universities` : 'No universities yet' }}</span>
                </div>
            </div>

            <div class="dashboard-hero__panel">
                <div class="dashboard-avatar">{{ (user.name || 'S').slice(0, 1).toUpperCase() }}</div>
                <p class="dashboard-hero__panel-copy">Use the cards below to move quickly without hunting through menus.</p>
            </div>
        </section>

        <section class="dashboard-section">
            <div class="section-heading-row">
                <div>
                    <p class="section-kicker">My universities</p>
                    <h2 class="section-title">Your current spaces</h2>
                </div>

                <button class="secondary-button" @click="router.push({ name: 'UniversitiesMine' })">View all</button>
            </div>

            <p v-if="dashboardMessage" class="dashboard-note">{{ dashboardMessage }}</p>

            <div v-if="isLoadingUniversities" class="dashboard-empty surface-card">
                Loading your universities...
            </div>

            <div v-else-if="!hasUniversities" class="dashboard-empty surface-card">
                <AppIcon name="users" />
                <div>
                    <h3>No universities yet</h3>
                    <p>Join a university from the catalog or create a new one to start building your space.</p>
                </div>
            </div>

            <div v-else class="dashboard-universities">
                <article v-for="item in displayedUniversities" :key="item.name" class="dashboard-university surface-card hover-lift">
                    <div class="dashboard-university__top">
                        <AppIcon name="university" />
                        <span class="pill" :class="item.isOpened ? 'pill--accent' : 'pill--pink'">{{ item.isOpened ? 'Open' : 'Private' }}</span>
                    </div>

                    <h3 class="dashboard-university__title">{{ item.name }}</h3>
                    <p class="section-copy dashboard-university__copy">{{ item.description }}</p>

                    <p class="dashboard-university__address">
                        {{ item.address?.country || 'Country N/A' }} · {{ item.address?.city || 'City N/A' }} · {{ item.address?.street || 'Street N/A' }}
                    </p>

                    <button class="secondary-button dashboard-university__open" @click="router.push({ name: 'UniversityCourses', params: { name: item.name } })">
                        Open university
                    </button>
                </article>
            </div>
        </section>

        <section class="dashboard-section">
            <div class="section-heading-row">
                <div>
                    <p class="section-kicker">Actions</p>
                    <h2 class="section-title">What do you want to do next?</h2>
                </div>
            </div>

            <div class="dashboard-grid">
                <article v-for="action in quickActions" :key="action.title" class="dashboard-card surface-card hover-lift">
                    <AppIcon :name="action.icon" />
                    <h3 class="dashboard-card__title">{{ action.title }}</h3>
                    <p class="section-copy">{{ action.copy }}</p>
                    <button class="submit-button dashboard-action" @click="router.push({ name: action.routeName })">
                        {{ action.button }}
                    </button>
                </article>
            </div>
        </section>
    </main>
</template>

<style scoped>
.dashboard-shell {
    width: min(1120px, calc(100% - 2rem));
    margin: 0 auto;
    padding: 1.5rem 0 2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.dashboard-hero {
    display: flex;
    align-items: stretch;
    justify-content: space-between;
    gap: 1rem;
    padding: 1.4rem;
}

.dashboard-hero__copy {
    display: flex;
    flex-direction: column;
    gap: 0.95rem;
}

.dashboard-hero__chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.65rem;
}

.dashboard-hero__panel {
    min-width: 14rem;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    gap: 0.8rem;
    padding: 1rem;
    border-radius: 1.2rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.08), rgba(255, 95, 162, 0.06));
    border: 1px solid rgba(143, 44, 226, 0.08);
}

.dashboard-avatar {
    width: 3rem;
    height: 3rem;
    border-radius: 1rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    color: #fff;
    font-size: 1.1rem;
    font-weight: 800;
}

.dashboard-hero__panel-copy {
    margin: 0;
    color: var(--ttl-text-secondary);
    line-height: 1.6;
}

.dashboard-section {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.section-heading-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
}

.dashboard-note {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.dashboard-empty {
    padding: 1.1rem;
    display: flex;
    align-items: center;
    gap: 0.9rem;
    color: var(--ttl-text-secondary);
}

.dashboard-empty h3,
.dashboard-university__title,
.dashboard-card__title {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.dashboard-empty p,
.dashboard-university__copy {
    margin: 0;
}

.dashboard-universities {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.9rem;
}

.dashboard-university {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.dashboard-university__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.dashboard-university__title,
.dashboard-card__title {
    font-size: 1.08rem;
}

.dashboard-university__address {
    margin: 0;
    color: var(--ttl-text-muted);
    font-size: 0.88rem;
}

.dashboard-university__open {
    margin-top: 0.2rem;
}

.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 1rem;
}

.dashboard-card {
    padding: 1.15rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.dashboard-action {
    margin-top: auto;
    width: 100%;
}

@media (max-width: 900px) {
    .dashboard-hero,
    .section-heading-row {
        flex-direction: column;
        align-items: flex-start;
    }

    .dashboard-universities,
    .dashboard-grid {
        grid-template-columns: 1fr;
    }
}
</style>