<script setup>
import { computed } from 'vue';

const props = defineProps({
    lesson: {
        type: Object,
        required: true,
    },
    isActive: {
        type: Boolean,
        default: false,
    },
    canManage: {
        type: Boolean,
        default: false,
    },
    disableActions: {
        type: Boolean,
        default: false,
    },
});

const emit = defineEmits(['open', 'edit', 'delete']);

const orderLabel = computed(() => {
    const raw = props.lesson?.orderNumber ?? props.lesson?.OrderNumber ?? 0;
    const value = Number(raw);
    return Number.isInteger(value) && value > 0 ? `#${value}` : '#-';
});

const markdownLabel = computed(() => {
    const raw = props.lesson?.isMarkdown ?? props.lesson?.IsMarkdown;
    return raw ? 'Markdown' : 'Plain text';
});

function triggerOpen() {
    emit('open', props.lesson);
}

function triggerEdit() {
    emit('edit', props.lesson);
}

function triggerDelete() {
    emit('delete', props.lesson);
}
</script>

<template>
    <article class="surface-card hover-lift lesson-card" :class="{ 'lesson-card--active': isActive }">
        <div class="lesson-card__top">
            <h3>{{ lesson.title || lesson.Title }}</h3>
            <span class="pill">{{ orderLabel }}</span>
        </div>

        <p class="section-copy lesson-card__meta">{{ markdownLabel }}</p>

        <div class="lesson-card__actions">
            <button class="secondary-button lesson-card__button" type="button" :disabled="disableActions" @click="triggerOpen">
                Open lesson
            </button>

            <button
                v-if="canManage"
                class="secondary-button lesson-card__button"
                type="button"
                :disabled="disableActions"
                @click="triggerEdit"
            >
                Edit
            </button>

            <button
                v-if="canManage"
                class="secondary-button lesson-card__button lesson-card__button--danger"
                type="button"
                :disabled="disableActions"
                @click="triggerDelete"
            >
                Delete
            </button>
        </div>
    </article>
</template>

<style scoped>
.lesson-card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
    transition: border-color var(--ttl-transition-base), box-shadow var(--ttl-transition-base);
}

.lesson-card--active {
    border-color: rgba(143, 44, 226, 0.32);
    box-shadow: 0 0 0 3px rgba(143, 44, 226, 0.12);
}

.lesson-card__top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.65rem;
}

.lesson-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
    font-size: 1rem;
}

.lesson-card__meta {
    margin: 0;
}

.lesson-card__actions {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.55rem;
}

.lesson-card__button {
    min-height: 2.25rem;
    font-size: 0.82rem;
}

.lesson-card__button--danger {
    color: var(--ttl-danger);
    border-color: rgba(241, 59, 113, 0.35);
}

@media (max-width: 700px) {
    .lesson-card__actions {
        grid-template-columns: 1fr;
    }
}
</style>
