<script setup>
import { reactive, watch } from 'vue';

const props = defineProps({
    mode: {
        type: String,
        default: 'create',
        validator: (value) => ['create', 'edit'].includes(value),
    },
    initialTitle: {
        type: String,
        default: '',
    },
    initialDescription: {
        type: String,
        default: '',
    },
    isSubmitting: {
        type: Boolean,
        default: false,
    },
    errorMessage: {
        type: String,
        default: '',
    },
});

const emit = defineEmits(['submit', 'cancel']);

const form = reactive({
    title: props.initialTitle,
    description: props.initialDescription,
});

watch(
    () => [props.initialTitle, props.initialDescription],
    ([title, description]) => {
        form.title = title;
        form.description = description;
    }
);

function validateForm() {
    const title = form.title.trim();
    const description = form.description.trim();

    if (!title) {
        return 'Course title is required.';
    }

    if (!description) {
        return 'Course description is required.';
    }

    if (title.length > 100) {
        return 'Course title must be 100 characters or less.';
    }

    if (description.length > 2000) {
        return 'Course description must be 2000 characters or less.';
    }

    return '';
}

function handleSubmit() {
    const validationError = validateForm();
    if (validationError) {
        emit('submit', {
            validationError,
            title: form.title,
            description: form.description,
        });
        return;
    }

    emit('submit', {
        validationError: '',
        title: form.title.trim(),
        description: form.description.trim(),
    });
}

function handleCancel() {
    emit('cancel');
}
</script>

<template>
    <article class="surface-card course-form-panel">
        <div class="course-form-panel__heading">
            <p class="section-kicker">{{ mode === 'create' ? 'Teacher action' : 'Course update' }}</p>
            <h3>{{ mode === 'create' ? 'Create a new course' : 'Edit course details' }}</h3>
        </div>

        <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>

        <div class="field-group">
            <label for="course-title" class="field-label">Title</label>
            <input
                id="course-title"
                v-model="form.title"
                class="input-field"
                type="text"
                maxlength="100"
                placeholder="Intro to Data Structures"
                :disabled="isSubmitting"
            />
        </div>

        <div class="field-group">
            <label for="course-description" class="field-label">Description</label>
            <textarea
                id="course-description"
                v-model="form.description"
                class="input-field course-form-panel__textarea"
                maxlength="2000"
                placeholder="Describe the learning plan, expected skills, and semester direction."
                :disabled="isSubmitting"
            />
        </div>

        <div class="course-form-panel__actions">
            <button class="submit-button" type="button" :disabled="isSubmitting" @click="handleSubmit">
                {{ isSubmitting ? 'Saving...' : mode === 'create' ? 'Create course' : 'Save changes' }}
            </button>

            <button class="secondary-button" type="button" :disabled="isSubmitting" @click="handleCancel">
                {{ mode === 'create' ? 'Clear form' : 'Cancel editing' }}
            </button>
        </div>
    </article>
</template>

<style scoped>
.course-form-panel {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
}

.course-form-panel__heading {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.course-form-panel__heading h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.course-form-panel__textarea {
    min-height: 7.8rem;
    resize: vertical;
}

.state-message {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.state-message--error {
    color: var(--ttl-danger);
}

.course-form-panel__actions {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.65rem;
}

@media (max-width: 640px) {
    .course-form-panel__actions {
        grid-template-columns: 1fr;
    }
}
</style>
