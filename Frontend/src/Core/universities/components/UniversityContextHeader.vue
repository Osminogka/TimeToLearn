<script setup>
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const router = useRouter();

const props = defineProps({
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
});

const emit = defineEmits(['join']);

const isTeacher = computed(() => isTeacherRole(user.value.role));
const currentRoleLabel = computed(() => isTeacher.value ? 'Teacher' : 'Student');

const tabs = computed(() => [
    { key: 'courses', label: 'Courses', icon: 'courses', routeName: 'UniversityCourses' },
    { key: 'info', label: 'University info', icon: 'info', routeName: 'UniversityInfo' },
    { key: 'forums', label: 'Forums', icon: 'forum', routeName: 'UniversityForums' },
]);

function triggerJoin() {
    emit('join');
}

function goDashboard() {
    router.push({ name: 'Dashboard' });
}

function goMyUniversities() {
    router.push({ name: 'UniversitiesMine' });
}

function goCatalog() {
    router.push({ name: 'UniversitiesAll' });
}
</script>

<template>
    <header class="context-header surface-card surface-card--raised">
        <div class="context-header__top">
            <div class="context-header__title-wrap">
                <AppIcon name="university" />

                <div>
                    <p class="section-kicker">University workspace</p>
                    <h1 class="section-title">{{ universityName }}</h1>
                    <p class="section-copy">{{ subtitle || 'Stay focused on one university space with clear sections for study and collaboration.' }}</p>
                </div>
            </div>

            <div class="context-header__meta">
                <span class="pill" :class="isOpened ? 'pill--accent' : 'pill--pink'">
                    {{ isOpened ? 'Open university' : 'Private university' }}
                </span>
                <span class="pill"><AppIcon name="role" />{{ currentRoleLabel }}</span>
                <span class="pill" :class="isMember ? 'pill--accent' : ''">{{ isMember ? 'Member' : 'Not a member' }}</span>
            </div>
        </div>

        <div class="context-header__bottom">
            <div class="context-nav-actions" aria-label="Workspace quick navigation">
                <button class="secondary-button context-nav-actions__button" type="button" @click="goDashboard">
                    Dashboard
                </button>
                <button class="secondary-button context-nav-actions__button" type="button" @click="goMyUniversities">
                    My universities
                </button>
                <button class="secondary-button context-nav-actions__button" type="button" @click="goCatalog">
                    Catalog
                </button>
            </div>

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
    justify-content: space-between;
    gap: 0.8rem;
}

.context-nav-actions {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0.55rem;
    min-width: 12.5rem;
}

.context-nav-actions__button {
    min-height: 2.6rem;
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

    .context-nav-actions {
        grid-template-columns: repeat(3, minmax(0, 1fr));
        width: 100%;
        min-width: 0;
    }
}

@media (max-width: 680px) {
    .context-nav-actions,
    .context-nav {
        grid-template-columns: 1fr;
    }
}
</style>
