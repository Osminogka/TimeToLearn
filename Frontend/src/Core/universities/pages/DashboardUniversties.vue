<script setup>
import authUtils from '@/Shared/services/utils';
import { useRouter } from 'vue-router';

const router = useRouter();

const quickActions = [
    {
        title: 'Browse all universities',
        copy: 'Discover open and private campuses and review where you want to study.',
        routeName: 'UniversitiesAll',
        button: 'Open catalog',
    },
    {
        title: 'Your university spaces',
        copy: 'See all universities where you are already a member and keep context in one place.',
        routeName: 'UniversitiesMine',
        button: 'View my universities',
    },
    {
        title: 'Create a university',
        copy: 'Set up a new learning community with a clear description and visibility settings.',
        routeName: 'UniversitiesCreate',
        button: 'Create now',
    },
    {
        title: 'Manage account and role',
        copy: 'Update profile details, switch between student and teacher roles, and verify your degree.',
        routeName: 'AccountManagement',
        button: 'Open account',
    },
];

function logout(){
    authUtils.clearToken();
    window.location.reload();
}
</script>

<template>
    <main class="dashboard-shell">
        <section class="dashboard-hero surface-card surface-card--raised">
            <div>
                <p class="section-kicker">Welcome back</p>
                <h1 class="section-title">Your learning dashboard is ready.</h1>
                <p class="section-copy">
                    Pick up your next lesson, jump into a forum, or review your university spaces from a clean overview.
                </p>
            </div>

            <button @click="logout" class="secondary-button dashboard-logout">Logout</button>
        </section>

        <section class="dashboard-grid">
            <article v-for="action in quickActions" :key="action.title" class="dashboard-card surface-card hover-lift">
                <p class="section-kicker">Quick action</p>
                <h2 class="dashboard-card__title">{{ action.title }}</h2>
                <p class="section-copy">{{ action.copy }}</p>
                <button class="secondary-button dashboard-action" @click="router.push({ name: action.routeName })">
                    {{ action.button }}
                </button>
            </article>
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
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 1.4rem;
}

.dashboard-logout {
    flex: 0 0 auto;
}

.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 1rem;
}

.dashboard-card {
    padding: 1.3rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.dashboard-card__title {
    margin: 0 0 0.55rem;
    color: var(--ttl-text-primary);
    font-size: 1.15rem;
    letter-spacing: -0.02em;
}

.dashboard-action {
    margin-top: auto;
    width: 100%;
}

@media (max-width: 900px) {
    .dashboard-grid {
        grid-template-columns: 1fr;
    }

    .dashboard-hero {
        flex-direction: column;
        align-items: start;
    }
}
</style>