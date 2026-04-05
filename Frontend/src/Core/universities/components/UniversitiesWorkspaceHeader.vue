<script setup>
import { computed } from 'vue';

const props = defineProps({
    title: {
        type: String,
        required: true,
    },
    subtitle: {
        type: String,
        required: true,
    },
    active: {
        type: String,
        required: true,
    },
});

const links = [
    { key: 'all', label: 'All universities', routeName: 'UniversitiesAll' },
    { key: 'mine', label: 'My universities', routeName: 'UniversitiesMine' },
    { key: 'create', label: 'Create university', routeName: 'UniversitiesCreate' },
    { key: 'account', label: 'Account', routeName: 'AccountManagement' },
];

const activeLabel = computed(() => links.find((link) => link.key === props.active)?.label || 'Workspace');
</script>

<template>
    <header class="workspace-header surface-card surface-card--raised">
        <div class="workspace-header__top">
            <div>
                <p class="section-kicker">University workspace</p>
                <h1 class="section-title">{{ title }}</h1>
                <p class="section-copy">{{ subtitle }}</p>
            </div>

            <span class="pill pill--accent">{{ activeLabel }}</span>
        </div>

        <nav class="workspace-nav" aria-label="University workspace navigation">
            <router-link
                v-for="link in links"
                :key="link.key"
                :to="{ name: link.routeName }"
                class="workspace-nav__link"
                :class="{ 'workspace-nav__link--active': active === link.key }"
            >
                {{ link.label }}
            </router-link>
        </nav>
    </header>
</template>

<style scoped>
.workspace-header {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.workspace-header__top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
}

.workspace-nav {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 0.65rem;
}

.workspace-nav__link {
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 2.6rem;
    border-radius: 0.8rem;
    background: rgba(143, 44, 226, 0.06);
    border: 1px solid transparent;
    color: var(--ttl-accent-dark);
    font-size: 0.9rem;
    font-weight: 700;
    transition: transform var(--ttl-transition-fast), border-color var(--ttl-transition-base), background var(--ttl-transition-base);
}

.workspace-nav__link:hover {
    transform: translateY(-1px);
}

.workspace-nav__link--active {
    background: rgba(143, 44, 226, 0.14);
    border-color: rgba(143, 44, 226, 0.25);
}

@media (max-width: 880px) {
    .workspace-nav {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

@media (max-width: 560px) {
    .workspace-nav {
        grid-template-columns: 1fr;
    }

    .workspace-header__top {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
