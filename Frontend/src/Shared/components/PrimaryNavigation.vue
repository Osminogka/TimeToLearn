<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AppIcon from './AppIcon.vue';
import { isTeacherRole, user } from '@/Shared/services/utils';

const route = useRoute();

const isTeacher = computed(() => isTeacherRole(user.value.role));

const navItems = computed(() => {
    const base = [
        { key: 'mine', label: 'My', routeName: 'UniversitiesMine', icon: 'users' },
        { key: 'all', label: 'Explore', routeName: 'UniversitiesAll', icon: 'join' },
    ];

    if (isTeacher.value) {
        base.push({ key: 'create', label: 'Create', routeName: 'UniversitiesCreate', icon: 'create' });
    }

    base.push({ key: 'account', label: 'Account', routeName: 'AccountManagement', icon: 'account' });
    return base;
});

function isActive(routeName) {
    if (routeName === 'UniversitiesAll') {
        return route.name === 'UniversitiesAll';
    }

    if (routeName === 'UniversitiesMine') {
        return route.name === 'UniversitiesMine';
    }

    if (routeName === 'UniversitiesCreate') {
        return route.name === 'UniversitiesCreate';
    }

    if (routeName === 'AccountManagement') {
        return route.name === 'AccountManagement';
    }

    return false;
}
</script>

<template>
    <div>
        <header class="primary-nav surface-card" aria-label="Primary navigation">
            <nav class="primary-nav__links">
                <router-link
                    v-for="item in navItems"
                    :key="item.key"
                    :to="{ name: item.routeName }"
                    class="primary-nav__link"
                    :class="{ 'primary-nav__link--active': isActive(item.routeName) }"
                >
                    <AppIcon :name="item.icon" />
                    <span>{{ item.label }}</span>
                </router-link>
            </nav>
        </header>

        <nav class="bottom-nav" aria-label="Bottom navigation">
            <router-link
                v-for="item in navItems"
                :key="`bottom-${item.key}`"
                :to="{ name: item.routeName }"
                class="bottom-nav__link"
                :class="{ 'bottom-nav__link--active': isActive(item.routeName) }"
            >
                <AppIcon :name="item.icon" />
                <span>{{ item.label }}</span>
            </router-link>
        </nav>
    </div>
</template>

<style scoped>
.primary-nav {
    width: min(1120px, calc(100% - 2rem));
    margin: 0.8rem auto 0;
    padding: 0.7rem;
}

.primary-nav__links {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(0, 1fr));
    gap: 0.6rem;
}

.primary-nav__link,
.bottom-nav__link {
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    min-height: 2.75rem;
    padding: 0.6rem 0.9rem;
    border-radius: 0.85rem;
    border: 1px solid transparent;
    background: rgba(143, 44, 226, 0.06);
    color: var(--ttl-accent-dark);
    font-weight: 700;
    font-size: 0.9rem;
    transition: transform var(--ttl-transition-fast), border-color var(--ttl-transition-base), background var(--ttl-transition-base);
}

.primary-nav__link:hover,
.bottom-nav__link:hover {
    transform: translateY(-1px);
}

.primary-nav__link--active,
.bottom-nav__link--active {
    background: rgba(143, 44, 226, 0.16);
    border-color: rgba(143, 44, 226, 0.24);
}

.bottom-nav {
    display: none;
}

@media (max-width: 760px) {
    .primary-nav {
        display: none;
    }

    .bottom-nav {
        position: fixed;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 50;
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(0, 1fr));
        gap: 0.5rem;
        padding: 0.55rem 0.75rem calc(0.55rem + env(safe-area-inset-bottom));
        background: rgba(255, 255, 255, 0.92);
        border-top: 1px solid var(--ttl-border-subtle);
        backdrop-filter: blur(10px);
    }

    .bottom-nav__link {
        flex-direction: column;
        gap: 0.25rem;
        min-height: 3rem;
        font-size: 0.78rem;
        padding: 0.45rem 0.35rem;
    }
}
</style>
