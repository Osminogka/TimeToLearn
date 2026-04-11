<script setup>
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import UniversitiesWorkspaceHeader from '@/Core/universities/components/UniversitiesWorkspaceHeader.vue';
import roleApi from '@/Core/roles/services/roleApi';
import userApi from '../services/userApi';
import authUtils, { user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const profile = reactive({
    firstName: '',
    lastName: '',
    phone: '',
    address: {
        country: '',
        city: '',
        street: '',
    },
});

const roleState = reactive({
    isTeacher: false,
    isStudent: false,
});

const degree = ref('');
const isLoadingProfile = ref(false);
const isSavingProfile = ref(false);
const isSwitchingRole = ref(false);
const isVerifying = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const router = useRouter();

function normalizeValue(payload) {
    return payload.value || payload.Value || null;
}

function clearMessages() {
    errorMessage.value = '';
    successMessage.value = '';
}

async function loadRoleState() {
    const currentUser = authUtils.getCurrentUser();
    if (!currentUser?.email) {
        return;
    }

    const roleResponse = await roleApi.getRole(currentUser.email);
    roleState.isTeacher = roleResponse.isTeacher ?? roleResponse.IsTeacher ?? false;
    roleState.isStudent = roleResponse.isStudent ?? roleResponse.IsStudent ?? false;
}

async function loadProfile() {
    const currentUser = authUtils.getCurrentUser();
    if (!currentUser?.unique_name && !user.value.name) {
        return;
    }

    isLoadingProfile.value = true;
    clearMessages();

    try {
        const profileResponse = await userApi.getByName(user.value.name || currentUser.unique_name);
        const value = normalizeValue(profileResponse);

        if (!value) {
            return;
        }

        profile.firstName = value.firstName || value.FirstName || '';
        profile.lastName = value.lastName || value.LastName || '';
        profile.phone = value.phoneNumber || value.PhoneNumber || '';
        profile.address.country = value.address?.country || value.Address?.Country || '';
        profile.address.city = value.address?.city || value.Address?.City || '';
        profile.address.street = value.address?.street || value.Address?.Street || '';
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load your profile.';
    } finally {
        isLoadingProfile.value = false;
    }
}

async function saveProfile() {
    isSavingProfile.value = true;
    clearMessages();

    try {
        const result = await userApi.updateProfile({
            firstName: profile.firstName.trim(),
            lastName: profile.lastName.trim(),
            phone: profile.phone.trim(),
            address: {
                country: profile.address.country.trim(),
                city: profile.address.city.trim(),
                street: profile.address.street.trim(),
            },
        });

        successMessage.value = result.message || result.Message || 'Profile updated.';
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to update profile.';
    } finally {
        isSavingProfile.value = false;
    }
}

async function becomeTeacher() {
    isSwitchingRole.value = true;
    clearMessages();

    try {
        const result = await roleApi.becomeTeacher();
        successMessage.value = result.message || result.Message || 'You are now a teacher.';
        await loadRoleState();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to switch role to teacher.';
    } finally {
        isSwitchingRole.value = false;
    }
}

async function becomeStudent() {
    isSwitchingRole.value = true;
    clearMessages();

    try {
        const result = await roleApi.becomeStudent();
        successMessage.value = result.message || result.Message || 'You are now a student.';
        await loadRoleState();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to switch role to student.';
    } finally {
        isSwitchingRole.value = false;
    }
}

async function verifyDegree() {
    if (!degree.value.trim()) {
        errorMessage.value = 'Degree field is required to verify teacher status.';
        return;
    }

    isVerifying.value = true;
    clearMessages();

    try {
        const result = await roleApi.verifyTeacher(degree.value.trim());
        successMessage.value = result.message || result.Message || 'Teacher verification submitted.';
        degree.value = '';
        await loadRoleState();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to verify teacher degree.';
    } finally {
        isVerifying.value = false;
    }
}

function logout() {
    authUtils.clearToken();
    router.push({ name: 'Main' });
}

onMounted(async () => {
    await loadRoleState();
    await loadProfile();
});
</script>

<template>
    <main class="page-shell">
        <UniversitiesWorkspaceHeader
            active="account"
            title="Manage your account"
            subtitle="Update personal details and switch between student and teacher roles."
        />

        <section class="account-hero surface-card surface-card--raised">
            <div class="account-hero__profile">
                <span class="account-hero__avatar">{{ (user.name || 'U').slice(0, 1).toUpperCase() }}</span>
                <div>
                    <p class="section-kicker">Account</p>
                    <h2 class="section-title">{{ user.name || 'Your profile' }}</h2>
                    <p class="section-copy">{{ user.email || 'Your account details and role settings live here.' }}</p>
                </div>
            </div>

            <div class="account-hero__actions">
                <span class="pill pill--accent">
                    <AppIcon name="profile" />
                    {{ roleState.isTeacher ? 'Teacher' : roleState.isStudent ? 'Student' : 'No role yet' }}
                </span>
                <button class="secondary-button account-logout" type="button" @click="logout">
                    <AppIcon name="logout" />
                    Log out
                </button>
            </div>
        </section>

        <section class="account-grid">
            <article class="surface-card account-card">
                <div class="account-card__header">
                    <div>
                        <p class="section-kicker">Profile</p>
                        <h2 class="section-title">Personal information</h2>
                    </div>
                    <span class="pill">{{ isLoadingProfile ? 'Loading profile...' : 'Ready' }}</span>
                </div>

                <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
                <p v-else-if="successMessage" class="state-message state-message--success">{{ successMessage }}</p>

                <form class="account-form" @submit.prevent="saveProfile" novalidate>
                    <div class="split-fields">
                        <div class="field-group">
                            <label class="field-label" for="account-first-name">First name</label>
                            <input id="account-first-name" v-model="profile.firstName" class="input-field" type="text" maxlength="50" />
                        </div>

                        <div class="field-group">
                            <label class="field-label" for="account-last-name">Last name</label>
                            <input id="account-last-name" v-model="profile.lastName" class="input-field" type="text" maxlength="50" />
                        </div>
                    </div>

                    <div class="field-group">
                        <label class="field-label" for="account-phone">Phone</label>
                        <input id="account-phone" v-model="profile.phone" class="input-field" type="text" maxlength="50" />
                    </div>

                    <div class="field-group">
                        <label class="field-label">Address</label>
                        <div class="split-fields">
                            <input v-model="profile.address.country" class="input-field" type="text" placeholder="Country" />
                            <input v-model="profile.address.city" class="input-field" type="text" placeholder="City" />
                        </div>
                        <input v-model="profile.address.street" class="input-field" type="text" placeholder="Street" />
                    </div>

                    <button class="submit-button" type="submit" :disabled="isSavingProfile">
                        {{ isSavingProfile ? 'Saving...' : 'Save profile' }}
                    </button>
                </form>
            </article>

            <article class="surface-card account-card">
                <div class="account-card__header">
                    <div>
                        <p class="section-kicker">Role</p>
                        <h2 class="section-title">Role management</h2>
                    </div>
                    <span class="pill pill--accent">
                        {{ roleState.isTeacher ? 'Teacher' : roleState.isStudent ? 'Student' : 'No role yet' }}
                    </span>
                </div>

                <p class="section-copy">Use these controls to switch roles and submit your degree for teacher verification.</p>

                <div class="role-actions">
                    <button class="secondary-button" @click="becomeStudent" :disabled="isSwitchingRole">Become student</button>
                    <button class="secondary-button" @click="becomeTeacher" :disabled="isSwitchingRole">Become teacher</button>
                </div>

                <div class="field-group">
                    <label class="field-label" for="account-degree">Teacher degree</label>
                    <input
                        id="account-degree"
                        v-model="degree"
                        class="input-field"
                        type="text"
                        placeholder="BSc Computer Science, MIT"
                    />
                    <button class="submit-button" @click="verifyDegree" :disabled="isVerifying" type="button">
                        {{ isVerifying ? 'Verifying...' : 'Verify degree' }}
                    </button>
                </div>
            </article>
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

.account-hero {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 1.25rem;
}

.account-hero__profile {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.account-hero__avatar {
    width: 3.2rem;
    height: 3.2rem;
    border-radius: 1rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
    color: #fff;
    font-weight: 800;
    font-size: 1.1rem;
}

.account-hero__actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.account-logout {
    min-width: 10rem;
}

.account-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 1rem;
}

.account-card {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.account-card__header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
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

.account-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.account-form .field-group {
    position: relative;
}

.split-fields {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
}

.role-actions {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
}

@media (max-width: 900px) {
    .account-hero,
    .account-card__header {
        flex-direction: column;
        align-items: flex-start;
    }

    .account-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .split-fields,
    .role-actions {
        grid-template-columns: 1fr;
    }
}
</style>
