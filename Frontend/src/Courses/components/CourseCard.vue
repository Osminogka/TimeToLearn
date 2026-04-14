<script setup>
import { computed } from 'vue';

const props = defineProps({
    course: {
        type: Object,
        required: true,
    },
    canEdit: {
        type: Boolean,
        default: false,
    },
    disableActions: {
        type: Boolean,
        default: false,
    },
    canDelete: {
        type: Boolean,
        default: false,
    },
});

const emit = defineEmits(['edit', 'delete', 'open-lessons']);

const createdLabel = computed(() => {
    const value = props.course?.createdAt || props.course?.CreatedAt;
    if (!value) {
        return 'Creation date unavailable';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return 'Creation date unavailable';
    }

    return `Created ${date.toLocaleDateString()}`;
});

const updatedLabel = computed(() => {
    const value = props.course?.updatedAt || props.course?.UpdatedAt;
    if (!value) {
        return 'Not updated yet';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return 'Updated recently';
    }

    return `Updated ${date.toLocaleDateString()}`;
});

const lessonsCount = computed(() => {
    const raw = props.course?.lessonsCount ?? props.course?.LessonsCount ?? 0;
    const value = Number(raw);
    return Number.isFinite(value) ? value : 0;
});

function triggerEdit() {
    emit('edit', props.course);
}

function triggerOpenLessons() {
    emit('open-lessons', props.course);
}

function triggerDelete() {
    emit('delete', props.course);
}
</script>

<template>
    <article class="surface-card hover-lift course-card">
        <div class="course-card__top">
            <h3>{{ course.title || course.Title }}</h3>
            <span class="pill pill--accent">{{ lessonsCount }} lessons</span>
        </div>

        <p class="section-copy course-card__description">{{ course.description || course.Description }}</p>

        <div class="course-card__meta-grid">
            <p class="section-kicker">Teacher</p>
            <p class="course-card__meta-value">{{ course.teacherName || course.TeacherName || 'Unknown teacher' }}</p>
        </div>

        <div class="course-card__meta-grid">
            <p class="section-kicker">Timeline</p>
            <p class="course-card__meta-value">{{ createdLabel }} · {{ updatedLabel }}</p>
        </div>

        <div class="course-card__actions">
            <button class="secondary-button course-card__button" type="button" :disabled="disableActions" @click="triggerOpenLessons">
                Lessons workspace
            </button>

            <button
                v-if="canEdit"
                class="secondary-button course-card__button"
                type="button"
                :disabled="disableActions"
                @click="triggerEdit"
            >
                Edit course
            </button>

            <button
                v-if="canDelete"
                class="secondary-button course-card__button course-card__button--danger"
                type="button"
                :disabled="disableActions"
                @click="triggerDelete"
            >
                Delete course
            </button>
        </div>
    </article>
</template>

<style scoped>
.course-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.course-card__top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
}

.course-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
    font-size: 1.1rem;
}

.course-card__description {
    margin: 0;
}

.course-card__meta-grid {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.course-card__meta-value {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
    line-height: 1.5;
}

.course-card__actions {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.6rem;
}

.course-card__button {
    min-height: 2.4rem;
    padding: 0.65rem 0.8rem;
    font-size: 0.86rem;
}

.course-card__button--danger {
    color: var(--ttl-danger);
    border-color: rgba(241, 59, 113, 0.35);
}

@media (max-width: 640px) {
    .course-card__actions {
        grid-template-columns: 1fr;
    }
}
</style>
