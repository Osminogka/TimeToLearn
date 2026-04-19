<script setup>
import { computed } from 'vue';

const props = defineProps({
    lesson: {
        type: Object,
        required: true,
    },
    canManage: {
        type: Boolean,
        default: false,
    },
    isBusy: {
        type: Boolean,
        default: false,
    },
    disableMoveUp: {
        type: Boolean,
        default: false,
    },
    disableMoveDown: {
        type: Boolean,
        default: false,
    },
});

const emit = defineEmits(['open', 'edit', 'move-up', 'move-down']);

const title = computed(() => String(props.lesson?.title || props.lesson?.Title || 'Untitled lesson'));
const orderNumber = computed(() => Number(props.lesson?.orderNumber || props.lesson?.OrderNumber || 0));
const isCompleted = computed(() => Boolean(props.lesson?.isCompletedByCurrentUser ?? props.lesson?.IsCompletedByCurrentUser));

function triggerOpen() {
    emit('open', props.lesson);
}

function triggerEdit() {
    emit('edit', props.lesson);
}

function moveUp() {
    emit('move-up', props.lesson);
}

function moveDown() {
    emit('move-down', props.lesson);
}
</script>

<template>
    <article class="surface-card lesson-mobile-item">
        <div class="lesson-mobile-item__header">
            <p class="lesson-mobile-item__order">Lesson {{ orderNumber || '-' }}</p>
            <span v-if="isCompleted" class="pill pill--pink">Done</span>
        </div>

        <h3 class="lesson-mobile-item__title">{{ title }}</h3>

        <div class="lesson-mobile-item__actions">
            <button class="submit-button" type="button" :disabled="isBusy" @click="triggerOpen">Open</button>
            <button v-if="canManage" class="secondary-button" type="button" :disabled="isBusy" @click="triggerEdit">Edit</button>
        </div>

        <div v-if="canManage" class="lesson-mobile-item__reorder">
            <button class="secondary-button" type="button" :disabled="isBusy || disableMoveUp" @click="moveUp">Move up</button>
            <button class="secondary-button" type="button" :disabled="isBusy || disableMoveDown" @click="moveDown">Move down</button>
        </div>
    </article>
</template>

<style scoped>
.lesson-mobile-item {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.lesson-mobile-item__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.6rem;
}

.lesson-mobile-item__order {
    margin: 0;
    color: var(--ttl-accent-dark);
    font-size: 0.82rem;
    font-weight: 800;
    letter-spacing: 0.07em;
    text-transform: uppercase;
}

.lesson-mobile-item__title {
    margin: 0;
    color: var(--ttl-text-primary);
    font-family: var(--ttl-font-display);
    font-size: 1.06rem;
    line-height: 1.2;
    letter-spacing: -0.02em;
}

.lesson-mobile-item__actions,
.lesson-mobile-item__reorder {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.55rem;
}

.lesson-mobile-item__actions .submit-button,
.lesson-mobile-item__actions .secondary-button,
.lesson-mobile-item__reorder .secondary-button {
    min-height: 2.55rem;
}

@media (max-width: 420px) {
    .lesson-mobile-item__actions,
    .lesson-mobile-item__reorder {
        grid-template-columns: 1fr;
    }
}
</style>
