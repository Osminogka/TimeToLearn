<script setup>
import { computed, reactive, watch } from 'vue';
import lessonResourcesService from '@/Courses/services/lessonResourcesService';
import MarkdownEditor from '@/Courses/components/MarkdownEditor.vue';

const props = defineProps({
    mode: {
        type: String,
        default: 'create',
        validator: (value) => ['create', 'edit'].includes(value),
    },
    initialLesson: {
        type: Object,
        default: null,
    },
    initialResources: {
        type: Array,
        default: () => [],
    },
    initialQuizQuestions: {
        type: Array,
        default: () => [],
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
    title: '',
    content: '',
    isMarkdown: true,
    orderNumber: 1,
    resources: [],
    lessonQuizQuestions: [],
});

const resourceTypeOptions = [
    { value: 'video', label: 'Video' },
    { value: 'material', label: 'Material' },
    { value: 'article', label: 'Article' },
    { value: 'slides', label: 'Slides' },
    { value: 'repository', label: 'Repository' },
    { value: 'other', label: 'Other' },
];

function createEmptyResource() {
    return {
        title: '',
        url: '',
        type: 'material',
    };
}

function createEmptyLessonQuizQuestion() {
    return {
        questionId: 0,
        questionText: '',
        attemptPolicy: 'reattempt',
        optionTexts: ['', ''],
        correctOptionIndex: 0,
    };
}

const isEditingMode = computed(() => props.mode === 'edit');
const isMarkdownEditorActive = computed(() => {
    if (!isEditingMode.value) {
        return Boolean(form.isMarkdown);
    }

    const raw = props.initialLesson?.isMarkdown ?? props.initialLesson?.IsMarkdown;
    return Boolean(raw);
});

function applyInitialLesson() {
    form.title = props.initialLesson?.title || props.initialLesson?.Title || '';
    const sourceContent = props.initialLesson?.content || props.initialLesson?.Content || '';
    form.content = lessonResourcesService.removeEmbeddedResourcesFromContent(sourceContent);

    if (isEditingMode.value) {
        const lessonMarkdownValue = props.initialLesson?.isMarkdown ?? props.initialLesson?.IsMarkdown;
        form.isMarkdown = lessonMarkdownValue !== undefined ? Boolean(lessonMarkdownValue) : true;
    } else {
        form.isMarkdown = true;
    }

    const rawOrderNumber = props.initialLesson?.orderNumber ?? props.initialLesson?.OrderNumber ?? 1;
    const parsed = Number(rawOrderNumber);
    form.orderNumber = Number.isInteger(parsed) && parsed > 0 ? parsed : 1;

    form.resources = (props.initialResources || []).map((item) => ({
        title: item?.title || '',
        url: item?.url || '',
        type: item?.type || 'material',
    }));

    if (!form.resources.length) {
        form.resources = [createEmptyResource()];
    }

    const quizQuestions = Array.isArray(props.initialQuizQuestions) ? props.initialQuizQuestions : [];
    form.lessonQuizQuestions = quizQuestions.length
        ? quizQuestions.map((question) => {
            const rawOptions = question.options || question.Options || [];
            const optionTexts = rawOptions
                .map((item) => String(item?.optionText || item?.OptionText || '').trim())
                .filter((item) => item.length > 0);

            const correctOptionIndex = rawOptions.findIndex((item) => Boolean(item?.isCorrect ?? item?.IsCorrect));

            return {
                questionId: Number(question.id || question.Id || 0),
                questionText: String(question.questionText || question.QuestionText || ''),
                attemptPolicy: String(question.attemptPolicy || question.AttemptPolicy || 'reattempt').trim().toLowerCase() === 'single' ? 'single' : 'reattempt',
                optionTexts: optionTexts.length >= 2 ? optionTexts : ['', ''],
                correctOptionIndex: correctOptionIndex >= 0 ? correctOptionIndex : 0,
            };
        })
        : [createEmptyLessonQuizQuestion()];
}

watch(
    () => [props.initialLesson, props.mode, props.initialQuizQuestions],
    () => {
        applyInitialLesson();
    },
    { immediate: true }
);

function normalizeResourcesForSubmit() {
    return form.resources
        .map((item) => ({
            title: String(item?.title || '').trim(),
            url: String(item?.url || '').trim(),
            type: String(item?.type || 'other').trim().toLowerCase(),
        }))
        .filter((item) => item.url)
        .map((item) => ({
            title: item.title || item.url,
            url: item.url,
            type: item.type || 'other',
        }));
}

function normalizeLessonQuizQuestionsForSubmit() {
    return form.lessonQuizQuestions.map((question) => {
        const questionText = String(question.questionText || '').trim();
        const cleanOptions = (Array.isArray(question.optionTexts) ? question.optionTexts : [])
            .map((item) => String(item || '').trim())
            .filter((item) => item.length > 0);

        const correctOptionIndex = Number(question.correctOptionIndex || 0);
        const options = cleanOptions.map((item, index) => ({
            optionText: item,
            isCorrect: index === correctOptionIndex,
        }));

        return {
            questionId: Number(question.questionId || 0),
            questionText,
            attemptPolicy: String(question.attemptPolicy || 'reattempt').trim().toLowerCase() === 'single' ? 'single' : 'reattempt',
            options,
        };
    });
}

function addResource() {
    form.resources.push(createEmptyResource());
}

function removeResource(index) {
    if (form.resources.length <= 1) {
        form.resources = [createEmptyResource()];
        return;
    }

    form.resources.splice(index, 1);
}

function addLessonQuizQuestion() {
    form.lessonQuizQuestions.push(createEmptyLessonQuizQuestion());
}

function removeLessonQuizQuestion(index) {
    if (form.lessonQuizQuestions.length <= 1) {
        return;
    }

    form.lessonQuizQuestions.splice(index, 1);
}

function addQuizOption(questionIndex) {
    form.lessonQuizQuestions[questionIndex].optionTexts.push('');
}

function removeQuizOption(questionIndex, optionIndex) {
    const question = form.lessonQuizQuestions[questionIndex];
    if (!question || question.optionTexts.length <= 2) {
        return;
    }

    question.optionTexts.splice(optionIndex, 1);
    if (question.correctOptionIndex >= question.optionTexts.length) {
        question.correctOptionIndex = 0;
    }
}

function validateForm() {
    const title = form.title.trim();
    const content = form.content.trim();
    const normalizedResources = normalizeResourcesForSubmit();
    const orderNumber = Number(form.orderNumber);
    const lessonQuizQuestions = normalizeLessonQuizQuestionsForSubmit();

    if (!title) {
        return 'Lesson title is required.';
    }

    if (!content) {
        return 'Lesson content is required.';
    }

    if (title.length > 200) {
        return 'Lesson title must be 200 characters or less.';
    }

    if (content.length > 10000) {
        return 'Lesson content must be 10000 characters or less.';
    }

    if (normalizedResources.some((item) => item.url.length > 500)) {
        return 'Each resource URL must be 500 characters or less.';
    }

    if (normalizedResources.some((item) => item.title.length > 120)) {
        return 'Each resource title must be 120 characters or less.';
    }

    if (!Number.isInteger(orderNumber) || orderNumber < 1) {
        return 'Order number must be a whole number starting from 1.';
    }

    if (!lessonQuizQuestions.length) {
        return 'At least one lesson quiz question is required.';
    }

    for (const [index, question] of lessonQuizQuestions.entries()) {
        if (!question.questionText) {
            return `Question ${index + 1}: question text is required.`;
        }

        if (question.questionText.length > 400) {
            return `Question ${index + 1}: question must be 400 characters or less.`;
        }

        if (question.options.length < 2) {
            return `Question ${index + 1}: at least 2 answer variants are required.`;
        }

        if (question.options.some((item) => item.optionText.length > 300)) {
            return `Question ${index + 1}: each variant must be 300 characters or less.`;
        }

        if (question.options.filter((item) => item.isCorrect).length !== 1) {
            return `Question ${index + 1}: select exactly one correct variant.`;
        }
    }

    return '';
}

function handleSubmit() {
    const validationError = validateForm();

    emit('submit', {
        validationError,
        title: form.title.trim(),
        content: form.content.trim(),
        isMarkdown: Boolean(form.isMarkdown),
        resources: normalizeResourcesForSubmit(),
        orderNumber: Number(form.orderNumber),
        lessonQuizQuestions: normalizeLessonQuizQuestionsForSubmit(),
    });
}

function handleCancel() {
    emit('cancel');
}
</script>

<template>
    <article class="surface-card lesson-form-panel">
        <div class="lesson-form-panel__heading">
            <p class="section-kicker">{{ isEditingMode ? 'Lesson update' : 'Teacher action' }}</p>
            <h3>{{ isEditingMode ? 'Edit lesson' : 'Create lesson' }}</h3>
        </div>

        <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>

        <div class="field-group">
            <label for="lesson-title" class="field-label">Title</label>
            <input
                id="lesson-title"
                v-model="form.title"
                class="input-field"
                type="text"
                maxlength="200"
                placeholder="Week 1: Arrays and complexity"
                :disabled="isSubmitting"
            />
        </div>

        <div class="field-group">
            <label for="lesson-order" class="field-label">Order number</label>
            <input
                id="lesson-order"
                v-model.number="form.orderNumber"
                class="input-field"
                type="number"
                min="1"
                :disabled="isSubmitting"
            />
        </div>

        <div v-if="!isEditingMode" class="field-group lesson-form-panel__checkbox-row">
            <label class="field-label lesson-form-panel__checkbox-label">
                <input v-model="form.isMarkdown" type="checkbox" :disabled="isSubmitting" />
                Markdown content
            </label>
            <p class="section-copy">When enabled, the lesson body is rendered as formatted markdown.</p>
        </div>

        <div class="field-group">
            <label for="lesson-content" class="field-label">Content</label>
            <MarkdownEditor
                v-if="isMarkdownEditorActive"
                v-model="form.content"
                :disabled="isSubmitting"
                :maxlength="10000"
                placeholder="Write markdown content for this lesson."
            />
            <textarea
                v-else
                id="lesson-content"
                v-model="form.content"
                class="input-field lesson-form-panel__textarea"
                maxlength="10000"
                placeholder="Write a clear lesson plan, examples, and home assignment guidance."
                :disabled="isSubmitting"
            />
        </div>

        <div class="lesson-form-panel__resources">
            <div class="lesson-form-panel__resources-head">
                <p class="section-kicker">Resources</p>
                <button class="secondary-button" type="button" :disabled="isSubmitting" @click="addResource">Add resource</button>
            </div>

            <div class="lesson-form-panel__resource-grid" v-for="(resource, index) in form.resources" :key="index">
                <div class="field-group">
                    <label class="field-label">Type</label>
                    <select v-model="resource.type" class="input-field" :disabled="isSubmitting">
                        <option v-for="item in resourceTypeOptions" :key="item.value" :value="item.value">{{ item.label }}</option>
                    </select>
                </div>

                <div class="field-group">
                    <label class="field-label">Title</label>
                    <input
                        v-model="resource.title"
                        class="input-field"
                        type="text"
                        maxlength="120"
                        placeholder="Homework brief"
                        :disabled="isSubmitting"
                    />
                </div>

                <div class="field-group">
                    <label class="field-label">URL</label>
                    <input
                        v-model="resource.url"
                        class="input-field"
                        type="url"
                        maxlength="500"
                        placeholder="https://..."
                        :disabled="isSubmitting"
                    />
                </div>

                <div class="field-group lesson-form-panel__resource-remove">
                    <button class="secondary-button" type="button" :disabled="isSubmitting" @click="removeResource(index)">
                        Remove
                    </button>
                </div>
            </div>
        </div>

        <div class="lesson-form-panel__quiz">
            <div class="lesson-form-panel__quiz-head">
                <p class="section-kicker">Lesson mini quiz</p>
                <p class="section-copy">Quiz is configured together with the lesson.</p>
            </div>

            <div class="lesson-form-panel__quiz-questions">
                <article v-for="(question, questionIndex) in form.lessonQuizQuestions" :key="`lesson-form-question-${questionIndex}`" class="lesson-form-panel__quiz-question-card">
                    <div class="field-group">
                        <label class="field-label">Question {{ questionIndex + 1 }}</label>
                        <input
                            v-model="question.questionText"
                            class="input-field"
                            type="text"
                            maxlength="400"
                            placeholder="What is the time complexity of binary search?"
                            :disabled="isSubmitting"
                        />
                    </div>

                    <div class="field-group">
                        <label class="field-label">Attempt policy</label>
                        <select v-model="question.attemptPolicy" class="input-field" :disabled="isSubmitting">
                            <option value="reattempt">Reattempt allowed</option>
                            <option value="single">Single attempt (lock after first answer)</option>
                        </select>
                    </div>

                    <div class="lesson-form-panel__quiz-options">
                        <div v-for="(optionText, optionIndex) in question.optionTexts" :key="`lesson-quiz-option-${questionIndex}-${optionIndex}`" class="lesson-form-panel__quiz-option-row">
                            <input
                                v-model="question.optionTexts[optionIndex]"
                                class="input-field"
                                type="text"
                                maxlength="300"
                                :placeholder="`Variant ${optionIndex + 1}`"
                                :disabled="isSubmitting"
                            />
                            <label class="lesson-form-panel__quiz-correct">
                                <input
                                    v-model.number="question.correctOptionIndex"
                                    type="radio"
                                    :name="`lesson-quiz-correct-${questionIndex}`"
                                    :value="optionIndex"
                                    :disabled="isSubmitting"
                                />
                                Correct
                            </label>
                            <button
                                class="secondary-button"
                                type="button"
                                :disabled="isSubmitting || question.optionTexts.length <= 2"
                                @click="removeQuizOption(questionIndex, optionIndex)"
                            >
                                Remove
                            </button>
                        </div>
                    </div>

                    <div class="lesson-form-panel__quiz-actions">
                        <button class="secondary-button" type="button" :disabled="isSubmitting" @click="addQuizOption(questionIndex)">Add variant</button>
                        <button class="secondary-button" type="button" :disabled="isSubmitting || form.lessonQuizQuestions.length <= 1" @click="removeLessonQuizQuestion(questionIndex)">
                            Remove question
                        </button>
                    </div>
                </article>
            </div>

            <button class="secondary-button" type="button" :disabled="isSubmitting" @click="addLessonQuizQuestion">Add question</button>
        </div>

        <div class="lesson-form-panel__actions">
            <button class="submit-button" type="button" :disabled="isSubmitting" @click="handleSubmit">
                {{ isSubmitting ? 'Saving...' : isEditingMode ? 'Save lesson' : 'Create lesson' }}
            </button>

            <button class="secondary-button" type="button" :disabled="isSubmitting" @click="handleCancel">
                {{ isEditingMode ? 'Cancel editing' : 'Cancel' }}
            </button>
        </div>
    </article>
</template>

<style scoped>
.lesson-form-panel {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
}

.lesson-form-panel__heading {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.lesson-form-panel__heading h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.lesson-form-panel__checkbox-row {
    gap: 0.45rem;
}

.lesson-form-panel__checkbox-label {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
}

.lesson-form-panel__checkbox-row .section-copy {
    margin: 0;
}

.lesson-form-panel__textarea {
    min-height: 10rem;
    resize: vertical;
}

.lesson-form-panel__resources {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.lesson-form-panel__resources-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.lesson-form-panel__resource-grid {
    display: grid;
    grid-template-columns: 0.9fr 1fr 1.4fr auto;
    gap: 0.6rem;
    align-items: end;
}

.lesson-form-panel__resource-remove .secondary-button {
    min-height: 2.65rem;
}

.lesson-form-panel__actions {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.65rem;
}

.lesson-form-panel__quiz {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
    padding: 0.75rem;
    border-radius: var(--ttl-radius-md);
    border: 1px solid var(--ttl-border-subtle);
    background: var(--ttl-bg-surface-soft);
}

.lesson-form-panel__quiz-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.7rem;
}

.lesson-form-panel__quiz-head .section-copy {
    margin: 0;
}

.lesson-form-panel__quiz-options {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.lesson-form-panel__quiz-questions {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.lesson-form-panel__quiz-question-card {
    padding: 0.65rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.lesson-form-panel__quiz-option-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    gap: 0.55rem;
    align-items: center;
}

.lesson-form-panel__quiz-correct {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    color: var(--ttl-text-secondary);
    font-size: 0.85rem;
}

.lesson-form-panel__quiz-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.55rem;
}

.state-message {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.state-message--error {
    color: var(--ttl-danger);
}

@media (max-width: 760px) {
    .lesson-form-panel__actions {
        grid-template-columns: 1fr;
    }

    .lesson-form-panel__resources-head {
        flex-direction: column;
        align-items: stretch;
    }

    .lesson-form-panel__resource-grid {
        grid-template-columns: 1fr;
    }

    .lesson-form-panel__quiz-option-row {
        grid-template-columns: 1fr;
    }
}
</style>
