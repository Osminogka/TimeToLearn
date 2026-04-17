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
    progress: {
        type: Object,
        default: null,
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

const completedLessonsCount = computed(() => {
    const raw = props.progress?.completedLessons
        ?? props.progress?.CompletedLessons
        ?? props.course?.completedLessonsCount
        ?? props.course?.CompletedLessonsCount
        ?? 0;
    const value = Number(raw);
    return Number.isFinite(value) ? value : 0;
});

const completionPercent = computed(() => {
    const raw = props.progress?.percentComplete
        ?? props.progress?.PercentComplete
        ?? props.course?.completionPercent
        ?? props.course?.CompletionPercent
        ?? 0;
    const value = Number(raw);
    return Number.isFinite(value) ? Math.max(0, Math.min(100, value)) : 0;
});

const markLabel = computed(() => {
    const raw = props.progress?.mark ?? props.progress?.Mark ?? props.course?.currentUserMark ?? props.course?.CurrentUserMark;
    const value = Number(raw);
    if (!Number.isInteger(value) || value < 1 || value > 10) {
        return '';
    }

    return `${value}/10`;
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

        <div v-if="progress" class="course-card__meta-grid">
            <div class="course-card__progress-head">
                <p class="section-kicker">Your progress</p>
                <span v-if="markLabel" class="pill pill--pink">Mark {{ markLabel }}</span>
            </div>
            <p class="course-card__meta-value">{{ completedLessonsCount }} / {{ lessonsCount }} lessons completed</p>
            <div class="course-card__track" role="progressbar" :aria-valuenow="completionPercent" aria-valuemin="0" aria-valuemax="100">
                <div class="course-card__fill" :style="{ width: `${completionPercent}%` }" />
            </div>
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

.course-card__progress-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.55rem;
}

.course-card__meta-value {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
    line-height: 1.5;
}

.course-card__track {
    height: 0.56rem;
    border-radius: 0.6rem;
    background: rgba(143, 44, 226, 0.12);
    overflow: hidden;
}

.course-card__fill {
    height: 100%;
    border-radius: 0.6rem;
    background: linear-gradient(90deg, rgba(143, 44, 226, 0.85), rgba(255, 95, 162, 0.85));
    transition: width var(--ttl-transition-base);
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
