<script setup>
import { computed } from 'vue';
import AppIcon from '@/Shared/components/AppIcon.vue';

defineProps({
    universityName: {
        type: String,
        required: true,
    },
    activeTab: {
        type: String,
        required: true,
    },
    subtitle: {
        type: String,
        default: '',
    },
    isOpened: {
        type: Boolean,
        default: false,
    },
    isMember: {
        type: Boolean,
        default: false,
    },
    joinVisible: {
        type: Boolean,
        default: false,
    },
    joinLabel: {
        type: String,
        default: 'Enter university',
    },
    isJoining: {
        type: Boolean,
        default: false,
    },
    joinDisabled: {
        type: Boolean,
        default: false,
    },
    showNavigation: {
        type: Boolean,
        default: true,
    },
});

const emit = defineEmits(['join']);

const tabs = computed(() => [
    { key: 'courses', label: 'Courses', icon: 'courses', routeName: 'UniversityCourses' },
    { key: 'info', label: 'University info', icon: 'info', routeName: 'UniversityInfo' },
    { key: 'forums', label: 'Forums', icon: 'forum', routeName: 'UniversityForums' },
]);

function triggerJoin() {
    emit('join');
}

</script>

<template>
    <header class="context-header surface-card surface-card--raised">
        <div class="context-header__top">
            <div class="context-header__meta">
                <router-link 
                    to="/universities/my" 
                    class="secondary-button context-header__back" 
                    custom 
                    v-slot="{ navigate }"
                >
                    <button class="context-header__back-btn" @click="navigate" type="button" title="Back to my universities">
                        <AppIcon name="back" />
                        <span>Back</span>
                    </button>
                </router-link>
            </div>

            <div class="context-header__title-wrap">
                <AppIcon name="university" />
                <div>
                    <h1 class="section-title">{{ universityName }}</h1>
                    <p class="section-kicker">University workspace</p>
                </div>
            </div>
        </div>
        <div v-if="showNavigation" class="context-header__bottom">
            <nav class="context-nav" aria-label="University tabs">
                <router-link
                    v-for="tab in tabs"
                    :key="tab.key"
                    :to="{ name: tab.routeName, params: { name: universityName } }"
                    class="context-nav__link"
                    :class="{ 'context-nav__link--active': activeTab === tab.key }"
                >
                    <AppIcon :name="tab.icon" />
                    {{ tab.label }}
                </router-link>
            </nav>

            <button
                v-if="joinVisible"
                class="submit-button context-header__join"
                type="button"
                @click="triggerJoin"
                :disabled="joinDisabled || isJoining"
            >
                {{ isJoining ? 'Please wait...' : joinLabel }}
            </button>
        </div>
    </header>
</template>

<style scoped>
.context-header {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.context-header__top {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1rem;
}

.context-header__title-wrap {
    display: flex;
    align-items: flex-start;
    gap: 0.8rem;
}

.context-header__meta {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    flex-wrap: wrap;
    justify-content: flex-end;
}

.context-header__bottom {
    display: flex;
    align-items: stretch;
    justify-content: flex-start;
    gap: 0.8rem;
}

.context-nav {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.65rem;
    width: 100%;
}

.context-nav__link {
    text-decoration: none;
    min-height: 2.65rem;
    border-radius: 0.82rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.55rem;
    color: var(--ttl-accent-dark);
    font-size: 0.9rem;
    font-weight: 700;
    border: 1px solid transparent;
    background: rgba(143, 44, 226, 0.06);
    transition: transform var(--ttl-transition-fast), border-color var(--ttl-transition-base), background var(--ttl-transition-base);
}

.context-nav__link:hover {
    transform: translateY(-1px);
}

.context-nav__link--active {
    background: rgba(143, 44, 226, 0.16);
    border-color: rgba(143, 44, 226, 0.25);
}

.context-header__join {
    min-width: 12.2rem;
    flex: 0 0 auto;
}

.context-header__back {
    min-height: 2.4rem;
}

@media (max-width: 930px) {
    .context-header__top,
    .context-header__bottom {
        flex-direction: column;
        align-items: flex-start;
    }

    .context-header__meta {
        justify-content: flex-start;
    }

    .context-header__join {
        width: 100%;
    }
}

@media (max-width: 680px) {
    .context-nav {
        display: flex;
        overflow-x: auto;
        white-space: nowrap;
        padding-bottom: 0.2rem;
    }

    .context-header__bottom {
        gap: 0.65rem;
    }

    .context-nav__link {
        min-width: 9.5rem;
        flex: 0 0 auto;
    }
}

.context-header__back-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
}

.context-header__back-btn :deep(.app-icon) {
    width: 1.85rem;
    height: 1.85rem;
    border-radius: 0.65rem;
}

@media (max-width: 480px) {
    .context-header__back-btn span {
        display: none;
    }
}
</style>
