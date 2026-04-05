<script setup>
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import InfoCardGrid from '../components/InfoCardGrid.vue';

const router = useRouter();
const route = useRoute();

const isAuthFormVisible = computed(() => {
    return ['Login', 'Register'].includes(route.name);
});

const currentAuthLabel = computed(() => route.name === 'Register' ? 'Create your account' : 'Sign in');
const currentAuthSubtitle = computed(() => route.name === 'Register'
    ? 'Join your university space, save your progress, and keep every discussion in one place.'
    : 'Pick up where you left off and keep your study spaces, courses, and forums organized.');

const authHighlights = [
    'Clear structure for classes, communities, and follow-up tasks',
    'Made for students who want a fast, calm, mobile-friendly experience',
    'Simple sign-in and registration with a polished product feel',
];

const stats = [
    { value: '4', label: 'connected study areas' },
    { value: '1', label: 'focused workspace' },
    { value: '24/7', label: 'access from any device' },
];

const previewBlocks = [
    {
        title: 'Today’s focus',
        text: 'Finish one lesson, reply to one forum post, and keep momentum without switching tabs.',
        accent: 'linear-gradient(135deg, rgba(143, 44, 226, 0.96), rgba(255, 95, 162, 0.88))',
    },
    {
        title: 'Campus updates',
        text: 'New university announcements, reminders, and group activity stay easy to scan.',
        accent: 'linear-gradient(135deg, rgba(255, 95, 162, 0.92), rgba(255, 177, 94, 0.92))',
    },
    {
        title: 'Discussion flow',
        text: 'Forum replies and peer questions are organized so students can jump in quickly.',
        accent: 'linear-gradient(135deg, rgba(115, 120, 255, 0.92), rgba(143, 44, 226, 0.92))',
    },
];
</script>

<template>
    <div class="greeting-page">
        <header class="hero-header">
            <router-link to="/" class="brand-link" aria-label="Go to the Time to Learn home page">
                <span class="brand-mark">TL</span>
                <span class="brand-copy">
                    <strong>Time to Learn</strong>
                    <span>Student productivity, simplified</span>
                </span>
            </router-link>

            <nav class="hero-actions" aria-label="Authentication shortcuts">
                <router-link v-if="route.name !== 'Login'" :to="{ name: 'Login' }" class="nav-button nav-button--soft">
                    Log in
                </router-link>
                <router-link v-if="route.name !== 'Register'" :to="{ name: 'Register' }" class="nav-button nav-button--solid">
                    Create account
                </router-link>
            </nav>
        </header>

        <main class="greeting-main">
            <section class="hero-grid" :class="{ 'hero-grid--auth': isAuthFormVisible }">
                <div class="hero-copy">
                    <p class="section-kicker">Built for students and university communities</p>
                    <h1 class="gradient-title hero-title">A calmer way to keep learning, collaborating, and showing up.</h1>
                    <p class="hero-text">
                        Time to Learn turns coursework, university spaces, and forum discussions into one polished workspace
                        that feels modern, fast, and easy to return to every day.
                    </p>

                    <div class="hero-actions-row">
                        <router-link :to="{ name: 'Register' }" class="nav-button nav-button--solid hero-cta">
                            Start free
                        </router-link>
                        <a href="#features" class="nav-button nav-button--soft hero-cta-secondary">
                            Explore features
                        </a>
                    </div>

                    <div class="hero-pills" aria-label="Key benefits">
                        <span class="pill pill--accent">University-ready</span>
                        <span class="pill">Course + forum flow</span>
                        <span class="pill pill--pink">Light-only interface</span>
                    </div>

                    <div class="hero-stats" aria-label="Platform highlights">
                        <article v-for="stat in stats" :key="stat.label" class="stat-card surface-card hover-lift">
                            <strong>{{ stat.value }}</strong>
                            <span>{{ stat.label }}</span>
                        </article>
                    </div>
                </div>

                <div class="hero-panel" :class="{ 'hero-panel--auth': isAuthFormVisible }">
                    <Transition name="fade-scale" mode="out-in">
                        <div v-if="isAuthFormVisible" key="auth" class="auth-panel auth-card">
                            <div class="auth-panel__header">
                                <p class="section-kicker">{{ route.name === 'Register' ? 'Join the platform' : 'Welcome back' }}</p>
                                <h2 class="section-title">{{ currentAuthLabel }}</h2>
                                <p class="section-copy">{{ currentAuthSubtitle }}</p>
                            </div>

                            <router-view v-slot="{ Component }">
                                <Transition name="fade-rise" mode="out-in">
                                    <component :is="Component" />
                                </Transition>
                            </router-view>
                        </div>

                        <div v-else key="preview" class="preview-panel surface-card surface-card--raised">
                            <div class="preview-panel__top">
                                <div>
                                    <p class="section-kicker">Live student workspace</p>
                                    <h2 class="section-title">Everything important is visible at a glance.</h2>
                                </div>
                                <span class="pill pill--accent">Live</span>
                            </div>

                            <div class="preview-grid">
                                <article v-for="block in previewBlocks" :key="block.title" class="preview-card hover-lift">
                                    <span class="preview-card__badge" :style="{ background: block.accent }"></span>
                                    <h3>{{ block.title }}</h3>
                                    <p>{{ block.text }}</p>
                                </article>
                            </div>
                        </div>
                    </Transition>
                </div>
            </section>

            <section v-if="!isAuthFormVisible" id="features" class="content-stack">
                <div class="section-heading-row">
                    <div>
                        <p class="section-kicker">What students get</p>
                        <h2 class="section-title">A landing page that feels like a product, not a brochure.</h2>
                    </div>
                    <p class="section-copy section-heading-row__copy">
                        The interface is intentionally light, clear, and structured so the brand feels trustworthy to new users.
                    </p>
                </div>

                <InfoCardGrid />
            </section>

            <section v-else class="auth-support-grid">
                <article v-for="highlight in authHighlights" :key="highlight" class="support-card surface-card hover-lift">
                    <span class="support-card__dot"></span>
                    <p>{{ highlight }}</p>
                </article>
            </section>
        </main>
    </div>
</template>

<style scoped>
.greeting-page {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    padding: 1rem;
    gap: 1.25rem;
}

.hero-header,
.greeting-main {
    width: min(1180px, 100%);
    margin-inline: auto;
}

.hero-header {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 1rem 1.1rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-xl);
    background: var(--ttl-bg-overlay);
    backdrop-filter: blur(18px);
    box-shadow: var(--ttl-shadow-sm);
}

.brand-link {
    display: inline-flex;
    align-items: center;
    gap: 0.85rem;
    text-decoration: none;
}

.brand-mark {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.8rem;
    height: 2.8rem;
    border-radius: 0.95rem;
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    color: #fff;
    font-family: var(--ttl-font-display);
    font-size: 1rem;
    font-weight: 700;
    box-shadow: 0 14px 30px rgba(143, 44, 226, 0.22);
}

.brand-copy {
    display: flex;
    flex-direction: column;
    gap: 0.08rem;
}

.brand-copy strong {
    color: var(--ttl-text-primary);
    font-size: 1rem;
    font-weight: 800;
    letter-spacing: -0.02em;
}

.brand-copy span {
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
}

.hero-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
}

.nav-button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 2.9rem;
    padding: 0.8rem 1.1rem;
    border-radius: 0.9rem;
    text-decoration: none;
    font-weight: 800;
    transition: transform var(--ttl-transition-fast), background var(--ttl-transition-base), box-shadow var(--ttl-transition-base);
}

.nav-button:hover {
    transform: translateY(-1px);
}

.nav-button--solid {
    background: linear-gradient(90deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    color: var(--ttl-text-on-accent);
    box-shadow: 0 14px 30px rgba(143, 44, 226, 0.22);
}

.nav-button--soft {
    background: rgba(143, 44, 226, 0.08);
    color: var(--ttl-accent-dark);
}

.greeting-main {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    padding-bottom: 1.5rem;
}

.hero-grid {
    display: grid;
    grid-template-columns: 1.15fr 0.85fr;
    gap: 1.5rem;
    align-items: start;
}

.hero-copy {
    display: flex;
    flex-direction: column;
    gap: 1.15rem;
    padding: clamp(0.4rem, 1vw, 0.8rem) 0;
}

.hero-title {
    max-width: 12ch;
}

.hero-text {
    margin: 0;
    max-width: 58ch;
    color: var(--ttl-text-secondary);
    font-size: clamp(1rem, 1.4vw, 1.12rem);
    line-height: 1.75;
}

.hero-actions-row {
    display: flex;
    flex-wrap: wrap;
    gap: 0.85rem;
}

.hero-cta,
.hero-cta-secondary {
    min-width: 10.75rem;
}

.hero-pills {
    display: flex;
    flex-wrap: wrap;
    gap: 0.65rem;
}

.hero-stats {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.9rem;
}

.stat-card {
    padding: 1rem 1.05rem;
}

.stat-card strong {
    display: block;
    margin-bottom: 0.25rem;
    color: var(--ttl-text-primary);
    font-size: 1.35rem;
    line-height: 1;
    letter-spacing: -0.04em;
}

.stat-card span {
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
    line-height: 1.45;
}

.hero-panel {
    min-height: 100%;
}

.preview-panel,
.auth-panel {
    padding: 1.4rem;
}

.preview-panel__top,
.auth-panel__header {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    margin-bottom: 1.2rem;
}

.preview-grid {
    display: grid;
    gap: 0.85rem;
}

.preview-card {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 1rem;
    border-radius: 1rem;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.95), rgba(252, 245, 255, 0.98));
    border: 1px solid var(--ttl-border-subtle);
}

.preview-card__badge {
    width: 2.2rem;
    height: 0.35rem;
    border-radius: 999px;
}

.preview-card h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 1rem;
    letter-spacing: -0.02em;
}

.preview-card p {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.94rem;
    line-height: 1.65;
}

.content-stack {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding-top: 0.5rem;
}

.section-heading-row {
    display: flex;
    justify-content: space-between;
    gap: 1rem;
    align-items: end;
}

.section-heading-row__copy {
    max-width: 30rem;
}

.auth-support-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 1rem;
}

.support-card {
    display: flex;
    align-items: center;
    gap: 0.8rem;
    padding: 1rem 1.1rem;
}

.support-card__dot {
    width: 0.8rem;
    height: 0.8rem;
    border-radius: 999px;
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    box-shadow: 0 0 0 6px rgba(143, 44, 226, 0.1);
    flex: 0 0 auto;
}

.support-card p {
    margin: 0;
    color: var(--ttl-text-secondary);
    line-height: 1.55;
}

@media (max-width: 1024px) {
    .hero-grid,
    .auth-support-grid {
        grid-template-columns: 1fr;
    }

    .hero-title {
        max-width: 16ch;
    }
}

@media (max-width: 720px) {
    .greeting-page {
        padding: 0.75rem;
    }

    .hero-header {
        padding: 0.95rem;
    }

    .hero-actions,
    .hero-actions-row {
        width: 100%;
    }

    .nav-button,
    .hero-cta,
    .hero-cta-secondary {
        width: 100%;
    }

    .hero-stats,
    .section-heading-row {
        grid-template-columns: 1fr;
        flex-direction: column;
        align-items: start;
    }

    .hero-grid {
        gap: 1rem;
    }
}
</style>