<script setup>
import { computed } from 'vue';
import AppIcon from '@/Shared/components/AppIcon.vue';
import { isTeacherRole, user } from '@/Shared/services/utils';

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

const isTeacher = computed(() => isTeacherRole(user.value.role));

const currentRoleLabel = computed(() => isTeacher.value ? 'Teacher' : 'Student');
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
                    <p class="workspace-header__user-email">{{ currentRoleLabel }} · {{ user.email || 'Account active' }}</p>
                </div>
            </div>
        </div>

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

@media (max-width: 560px) {
    .workspace-header__top {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
