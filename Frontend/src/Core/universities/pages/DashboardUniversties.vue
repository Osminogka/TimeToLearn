<script setup>
import authUtils from '@/Shared/services/utils';

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
            <article class="dashboard-card surface-card hover-lift">
                <p class="section-kicker">Quick actions</p>
                <h2 class="dashboard-card__title">Stay in motion.</h2>
                <p class="section-copy">Return to the work you were already doing instead of hunting through menus.</p>
            </article>

            <article class="dashboard-card surface-card hover-lift">
                <p class="section-kicker">Community</p>
                <h2 class="dashboard-card__title">Connect with your peers.</h2>
                <p class="section-copy">Forum activity and university updates live in one place with the same visual language.</p>
            </article>

            <article class="dashboard-card surface-card hover-lift">
                <p class="section-kicker">Progress</p>
                <h2 class="dashboard-card__title">Clarity over clutter.</h2>
                <p class="section-copy">Track your learning at a glance without the heavy, dashboard-like noise common in school apps.</p>
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
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 1rem;
}

.dashboard-card {
    padding: 1.3rem;
}

.dashboard-card__title {
    margin: 0 0 0.55rem;
    color: var(--ttl-text-primary);
    font-size: 1.15rem;
    letter-spacing: -0.02em;
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