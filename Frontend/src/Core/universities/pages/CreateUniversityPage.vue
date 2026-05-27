<script setup>
import { computed, reactive, ref } from 'vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';

const form = reactive({
    name: '',
    description: '',
    isOpened: true,
    address: {
        country: '',
        city: '',
        street: '',
    },
});

const isSubmitting = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

authUtils.getCurrentUser();
const isTeacher = computed(() => isTeacherRole(user.value.role));

function resetMessages() {
    errorMessage.value = '';
    successMessage.value = '';
}

function validateForm() {
    if (!form.name.trim()) {
        return 'University name is required.';
    }

    if (!form.description.trim()) {
        return 'University description is required.';
    }

    return '';
}

async function submitCreateUniversity() {
    resetMessages();

    if (!isTeacher.value) {
        errorMessage.value = 'Only teachers can create universities. Switch role in Account Management.';
        return;
    }

    const validationError = validateForm();
    if (validationError) {
        errorMessage.value = validationError;
        return;
    }

    isSubmitting.value = true;

    try {
        const country = form.address.country.trim();
        const city = form.address.city.trim();
        const street = form.address.street.trim();
        const address = country || city || street ? { country, city, street } : null;

        const response = await universityApi.createUniversity({
            name: form.name.trim(),
            description: form.description.trim(),
            isOpened: form.isOpened,
            address,
        });

        successMessage.value = response.message || response.Message || 'University created successfully.';
        form.name = '';
        form.description = '';
        form.isOpened = true;
        form.address.country = '';
        form.address.city = '';
        form.address.street = '';
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to create university.';
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <main class="page-shell">
        <section class="content-panel surface-card">
            <div>
                <p class="section-kicker">New university</p>
                <h2 class="section-title">Create community</h2>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="successMessage" class="state-message state-message--success">{{ successMessage }}</p>

            <p v-if="!isTeacher" class="state-message state-message--warning">Teacher role required to create a university.</p>

            <form class="create-form" @submit.prevent="submitCreateUniversity" novalidate>
                <div class="field-group">
                    <label for="uni-name" class="field-label">University name</label>
                    <input id="uni-name" v-model="form.name" class="input-field" type="text" maxlength="50" placeholder="Digital Engineering Campus" :disabled="!isTeacher || isSubmitting" />
                </div>

                <div class="field-group">
                    <label for="uni-description" class="field-label">Description</label>
                    <textarea
                        id="uni-description"
                        v-model="form.description"
                        class="input-field create-form__textarea"
                        maxlength="500"
                        placeholder="Describe your university focus, students, and learning direction."
                        :disabled="!isTeacher || isSubmitting"
                    />
                </div>

                <div class="field-group">
                    <label class="field-label">Address</label>
                    <div class="address-grid">
                        <input v-model="form.address.country" class="input-field" type="text" placeholder="Country" :disabled="!isTeacher || isSubmitting" />
                        <input v-model="form.address.city" class="input-field" type="text" placeholder="City" :disabled="!isTeacher || isSubmitting" />
                        <input v-model="form.address.street" class="input-field" type="text" placeholder="Street" :disabled="!isTeacher || isSubmitting" />
                    </div>
                </div>

                <div class="field-group">
                    <label class="field-label">Access type</label>
                    <select v-model="form.isOpened" class="input-field" :disabled="!isTeacher || isSubmitting">
                        <option :value="true">Open university</option>
                        <option :value="false">Private university</option>
                    </select>
                </div>

                <button class="submit-button create-form__submit" type="submit" :disabled="!isTeacher || isSubmitting">
                    {{ isTeacher ? (isSubmitting ? 'Creating...' : 'Create university') : 'Teacher role required' }}
                </button>
            </form>
        </section>
    </main>
</template>

<style scoped>
.page-shell {
    width: min(1120px, calc(100% - 2rem));
    margin: 1.5rem auto 2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.content-panel {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.state-message {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.state-message--error {
    color: var(--ttl-danger);
}

.state-message--success {
    color: var(--ttl-success);
}

.state-message--warning {
    color: var(--ttl-accent-dark);
}

.create-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.create-form__textarea {
    min-height: 8.2rem;
    resize: vertical;
}

.address-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.75rem;
}

.create-form__submit {
    width: 100%;
}

@media (max-width: 760px) {
    .address-grid {
        grid-template-columns: 1fr;
    }
}
</style>
