<script setup>
import { computed } from 'vue';
import AppIcon from '@/Shared/components/AppIcon.vue';
import { user } from '@/Shared/services/utils';

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
    { key: 'all', label: 'Join university', routeName: 'UniversitiesAll', icon: 'join' },
    { key: 'mine', label: 'My universities', routeName: 'UniversitiesMine', icon: 'users' },
    { key: 'create', label: 'Create', routeName: 'UniversitiesCreate', icon: 'create' },
    { key: 'account', label: 'Profile', routeName: 'AccountManagement', icon: 'account' },
];
</script>

<template>
    <header class="workspace-header surface-card surface-card--raised">
        <div class="workspace-header__top">
            <div class="workspace-header__title">
                <AppIcon :name="active === 'all' ? 'join' : active === 'mine' ? 'users' : active === 'create' ? 'create' : 'account'" />
                <div>
                    <p class="section-kicker">University workspace</p>
                    <h1 class="section-title">{{ title }}</h1>
                    <p class="section-copy">{{ subtitle }}</p>
                </div>
            </div>

            <div class="workspace-header__user">
                <span class="workspace-header__avatar">{{ (user.name || 'U').slice(0, 1).toUpperCase() }}</span>
                <div>
                    <p class="workspace-header__user-name">{{ user.name || 'Student' }}</p>
                    <p class="workspace-header__user-email">{{ user.email || 'Account active' }}</p>
                </div>
            </div>
        </div>

        <nav class="workspace-nav" aria-label="University workspace navigation">
            <router-link
                v-for="link in links"
                :key="link.key"
                :to="{ name: link.routeName }"
                class="workspace-nav__link"
                :class="{ 'workspace-nav__link--active': active === link.key }"
            >
                <AppIcon :name="link.icon" />
                {{ link.label }}
            </router-link>
        </nav>
    </header>
</template>

<style scoped>
.workspace-header {
    padding: 1.15rem;
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

.workspace-header__title {
    display: flex;
    align-items: flex-start;
    gap: 0.85rem;
}

.workspace-header__user {
    display: inline-flex;
    align-items: center;
    gap: 0.8rem;
    padding: 0.75rem 0.9rem;
    border-radius: 1rem;
    background: rgba(143, 44, 226, 0.06);
    border: 1px solid rgba(143, 44, 226, 0.1);
}

.workspace-header__avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.4rem;
    height: 2.4rem;
    border-radius: 0.85rem;
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    color: #fff;
    font-weight: 800;
}

.workspace-header__user-name {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 0.95rem;
    font-weight: 800;
}

.workspace-header__user-email {
    margin: 0.1rem 0 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
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
    gap: 0.55rem;
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
