<script setup>
import { reactive, ref } from 'vue';
import UniversitiesWorkspaceHeader from '../components/UniversitiesWorkspaceHeader.vue';
import universityApi from '../services/universityApi';

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

    if (!form.address.country.trim() || !form.address.city.trim() || !form.address.street.trim()) {
        return 'Please fill in country, city, and street.';
    }

    return '';
}

async function submitCreateUniversity() {
    resetMessages();

    const validationError = validateForm();
    if (validationError) {
        errorMessage.value = validationError;
        return;
    }

    isSubmitting.value = true;

    try {
        const response = await universityApi.createUniversity({
            name: form.name.trim(),
            description: form.description.trim(),
            isOpened: form.isOpened,
            address: {
                country: form.address.country.trim(),
                city: form.address.city.trim(),
                street: form.address.street.trim(),
            },
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
        <UniversitiesWorkspaceHeader
            active="create"
            title="Create a university"
            subtitle="Launch a new learning community with clear information and access settings."
        />

        <section class="content-panel surface-card">
            <div>
                <p class="section-kicker">New university</p>
                <h2 class="section-title">Create community</h2>
                <p class="section-copy">Students discover your university from the shared catalog as soon as it is created.</p>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="successMessage" class="state-message state-message--success">{{ successMessage }}</p>

            <form class="create-form" @submit.prevent="submitCreateUniversity" novalidate>
                <div class="field-group">
                    <label for="uni-name" class="field-label">University name</label>
                    <input id="uni-name" v-model="form.name" class="input-field" type="text" maxlength="50" placeholder="Digital Engineering Campus" />
                </div>

                <div class="field-group">
                    <label for="uni-description" class="field-label">Description</label>
                    <textarea
                        id="uni-description"
                        v-model="form.description"
                        class="input-field create-form__textarea"
                        maxlength="500"
                        placeholder="Describe your university focus, students, and learning direction."
                    />
                </div>

                <div class="field-group">
                    <label class="field-label">Address</label>
                    <div class="address-grid">
                        <input v-model="form.address.country" class="input-field" type="text" placeholder="Country" />
                        <input v-model="form.address.city" class="input-field" type="text" placeholder="City" />
                        <input v-model="form.address.street" class="input-field" type="text" placeholder="Street" />
                    </div>
                </div>

                <div class="field-group">
                    <label class="field-label">Access type</label>
                    <div class="toggle-row">
                        <button
                            class="secondary-button toggle-row__item"
                            :class="{ 'toggle-row__item--active': form.isOpened }"
                            type="button"
                            @click="form.isOpened = true"
                        >
                            Open university
                        </button>
                        <button
                            class="secondary-button toggle-row__item"
                            :class="{ 'toggle-row__item--active': !form.isOpened }"
                            type="button"
                            @click="form.isOpened = false"
                        >
                            Private university
                        </button>
                    </div>
                </div>

                <button class="submit-button create-form__submit" type="submit" :disabled="isSubmitting">
                    {{ isSubmitting ? 'Creating...' : 'Create university' }}
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

.toggle-row {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
}

.toggle-row__item {
    width: 100%;
}

.toggle-row__item--active {
    background: rgba(143, 44, 226, 0.18);
}

.create-form__submit {
    width: 100%;
}

@media (max-width: 760px) {
    .address-grid,
    .toggle-row {
        grid-template-columns: 1fr;
    }
}
</style>
