<script setup>
import { computed, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import authApi from '../services/authApi';
import {
    collectSummaryMessages,
    mergeFieldErrors,
    toFriendlyAuthErrors,
    validateRegisterForm,
} from '../services/authFormHelpers';

const form = reactive({
    name: '',
    email: '',
    password: '',
});
const fieldErrors = reactive({
    name: [],
    email: [],
    password: [],
});
const generalErrors = ref([]);
const isSubmitting = ref(false);

const router = useRouter();

const hasAnyError = computed(() => summaryErrors.value.length > 0);
const summaryErrors = computed(() => collectSummaryMessages(fieldErrors, generalErrors.value));

function clearErrors() {
    fieldErrors.name = [];
    fieldErrors.email = [];
    fieldErrors.password = [];
    generalErrors.value = [];
}

function applyFieldErrors(nextErrors) {
    fieldErrors.name = nextErrors.name || [];
    fieldErrors.email = nextErrors.email || [];
    fieldErrors.password = nextErrors.password || [];
}

function validateForm() {
    const validationErrors = validateRegisterForm({
        name: form.name.trim(),
        email: form.email.trim(),
        password: form.password,
    });
    applyFieldErrors(validationErrors);
    return collectSummaryMessages(validationErrors, []).length === 0;
}

async function submitRegister() {
    clearErrors();
    const isValid = validateForm();
    if (!isValid) {
        return;
    }

    isSubmitting.value = true;
    try {
        const result = await authApi.register(form.name.trim(), form.email.trim(), form.password);
        if (result.success) {
            router.replace({ name: 'UniversitiesMine' });
            return;
        }

        const serverErrors = toFriendlyAuthErrors(result.message, 'register', {
            name: form.name,
            email: form.email,
            password: form.password,
        });
        applyFieldErrors(mergeFieldErrors(fieldErrors, serverErrors.fields));
        generalErrors.value = serverErrors.general;
    } catch (error) {
        const serverErrors = toFriendlyAuthErrors(error?.message || 'Registration failed', 'register', {
            name: form.name,
            email: form.email,
            password: form.password,
        });
        applyFieldErrors(mergeFieldErrors(fieldErrors, serverErrors.fields));
        generalErrors.value = serverErrors.general;
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="auth-form-shell">
        <div v-if="hasAnyError" class="error-summary" role="alert" aria-live="assertive">
            <p class="error-summary-title">We could not create your account:</p>
            <ul>
                <li v-for="message in summaryErrors" :key="message">{{ message }}</li>
            </ul>
        </div>

        <form class="auth-form" @submit.prevent="submitRegister" novalidate>
            <div class="field-group">
                <label for="register-name" class="field-label">Username</label>
                <input
                    id="register-name"
                    class="input-field"
                    :class="{ 'input-error': fieldErrors.name.length > 0 }"
                    v-model.trim="form.name"
                    type="text"
                    placeholder="Your public username"
                    autocomplete="username"
                    :aria-invalid="fieldErrors.name.length > 0"
                    :disabled="isSubmitting"
                />
                <ul v-if="fieldErrors.name.length > 0" class="field-error-list">
                    <li v-for="message in fieldErrors.name" :key="`name-${message}`">{{ message }}</li>
                </ul>
            </div>

            <div class="field-group">
                <label for="register-email" class="field-label">Email</label>
                <input
                    id="register-email"
                    class="input-field"
                    :class="{ 'input-error': fieldErrors.email.length > 0 }"
                    v-model.trim="form.email"
                    type="email"
                    placeholder="name@university.edu"
                    autocomplete="email"
                    :aria-invalid="fieldErrors.email.length > 0"
                    :disabled="isSubmitting"
                />
                <ul v-if="fieldErrors.email.length > 0" class="field-error-list">
                    <li v-for="message in fieldErrors.email" :key="`email-${message}`">{{ message }}</li>
                </ul>
            </div>

            <div class="field-group">
                <label for="register-password" class="field-label">Password</label>
                <input
                    id="register-password"
                    class="input-field"
                    :class="{ 'input-error': fieldErrors.password.length > 0 }"
                    v-model="form.password"
                    type="password"
                    placeholder="Create a strong password"
                    autocomplete="new-password"
                    :aria-invalid="fieldErrors.password.length > 0"
                    :disabled="isSubmitting"
                />
                <ul v-if="fieldErrors.password.length > 0" class="field-error-list">
                    <li v-for="message in fieldErrors.password" :key="`password-${message}`">{{ message }}</li>
                </ul>
            </div>

            <button class="submit-button auth-submit" type="submit" :disabled="isSubmitting">
                {{ isSubmitting ? 'Creating account...' : 'Register' }}
            </button>
        </form>
    </div>
</template>

<style scoped>
.auth-form-shell {
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
}

.auth-submit {
    width: 100%;
    margin-top: 0.25rem;
}
</style>
