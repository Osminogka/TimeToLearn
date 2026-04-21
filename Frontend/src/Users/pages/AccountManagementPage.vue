<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import roleApi from '@/Core/roles/services/roleApi';
import authApi from '@/Authentication/services/authApi';
import userApi from '../services/userApi';
import authUtils, { isTeacherRole, isStudentRole, user } from '@/Shared/services/utils';
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

const currentRoleLabel = computed(() => roleState.isTeacher ? 'Teacher' : roleState.isStudent ? 'Student' : 'No role yet');

function syncRoleFromToken() {
    authUtils.getCurrentUser();
    roleState.isTeacher = isTeacherRole(user.value.role);
    roleState.isStudent = isStudentRole(user.value.role) || !roleState.isTeacher;
}

const degree = ref('');
const roleTarget = ref('student');
const invites = ref([]);
const isLoadingProfile = ref(false);
const isLoadingInvites = ref(false);
const isSavingProfile = ref(false);
const isSwitchingRole = ref(false);
const isVerifying = ref(false);
const inviteActionMap = ref({});
const errorMessage = ref('');
const successMessage = ref('');
const router = useRouter();

function normalizeValue(payload) {
    return payload.value || payload.Value || null;
}

function normalizeItems(payload) {
    return payload.items || payload.Items || payload.enum || payload.Enum || [];
}

function clearMessages() {
    errorMessage.value = '';
    successMessage.value = '';
}

async function loadRoleState() {
    syncRoleFromToken();
    roleTarget.value = roleState.isTeacher ? 'student' : 'teacher';
}

function isInviteActionLoading(universityName, action) {
    return Boolean(inviteActionMap.value[`${action}:${universityName}`]);
}

function setInviteActionLoading(universityName, action, value) {
    inviteActionMap.value = {
        ...inviteActionMap.value,
        [`${action}:${universityName}`]: value,
    };
}

async function loadInvites() {
    isLoadingInvites.value = true;

    try {
        const response = await userApi.getInvites();
        invites.value = normalizeItems(response);
    } catch {
        invites.value = [];
    } finally {
        isLoadingInvites.value = false;
    }
}

async function refreshSessionToken() {
    const refreshed = await authApi.refreshToken();
    if (!refreshed.success) {
        throw new Error(refreshed.message || 'Could not refresh session token.');
    }

    syncRoleFromToken();
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
        await refreshSessionToken();
        successMessage.value = result.message || result.Message || 'You are now a teacher.';
        await loadRoleState();
        await loadInvites();
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
        await refreshSessionToken();
        successMessage.value = result.message || result.Message || 'You are now a student.';
        await loadRoleState();
        await loadInvites();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to switch role to student.';
    } finally {
        isSwitchingRole.value = false;
    }
}

async function switchRole() {
    if (roleTarget.value === 'teacher') {
        await becomeTeacher();
        return;
    }

    await becomeStudent();
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
        await refreshSessionToken();
        successMessage.value = result.message || result.Message || 'Teacher verification submitted.';
        degree.value = '';
        await loadRoleState();
        await loadInvites();
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

async function acceptInviteAsCurrentRole(universityName) {
    setInviteActionLoading(universityName, 'accept', true);
    clearMessages();

    try {
        const response = await userApi.acceptInvite(universityName);
        successMessage.value = response?.message || response?.Message || 'Invite accepted.';
        await loadInvites();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to accept invite.';
    } finally {
        setInviteActionLoading(universityName, 'accept', false);
    }
}

async function rejectInvite(universityName) {
    setInviteActionLoading(universityName, 'reject', true);
    clearMessages();

    try {
        const response = await userApi.rejectInvite(universityName);
        successMessage.value = response?.message || response?.Message || 'Invite rejected.';
        await loadInvites();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to reject invite.';
    } finally {
        setInviteActionLoading(universityName, 'reject', false);
    }
}

onMounted(async () => {
    await loadRoleState();
    await Promise.all([
        loadProfile(),
        loadInvites(),
    ]);
});
</script>

<template>
    <main class="page-shell">
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
                    <AppIcon name="profile" :boxed="false" />
                    {{ currentRoleLabel }}
                </span>
                <button class="secondary-button account-logout" type="button" @click="logout">
                    <AppIcon name="logout" :boxed="false" />
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
                        {{ currentRoleLabel }}
                    </span>
                </div>

                <div class="field-group">
                    <label class="field-label" for="role-target">Switch role</label>
                    <select id="role-target" v-model="roleTarget" class="input-field">
                        <option value="student">Student</option>
                        <option value="teacher">Teacher</option>
                    </select>
                    <button class="submit-button" @click="switchRole" :disabled="isSwitchingRole || roleTarget === currentRoleLabel.toLowerCase()" type="button">
                        {{ isSwitchingRole ? 'Switching...' : `Become ${roleTarget}` }}
                    </button>
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

            <article class="surface-card account-card">
                <div class="account-card__header">
                    <div>
                        <p class="section-kicker">University invites</p>
                        <h2 class="section-title">Pending invitations</h2>
                    </div>
                    <span class="pill">{{ isLoadingInvites ? 'Loading...' : `${invites.length} pending` }}</span>
                </div>

                <p class="section-copy" v-if="!invites.length && !isLoadingInvites">
                    You have no pending invites right now.
                </p>

                <div v-else class="invite-list">
                    <article v-for="universityName in invites" :key="`invite-${universityName}`" class="invite-card">
                        <div>
                            <p class="invite-card__title">{{ universityName }}</p>
                        </div>

                        <div class="invite-actions">
                            <button
                                class="submit-button invite-action-button invite-action-button--accept"
                                type="button"
                                @click="acceptInviteAsCurrentRole(universityName)"
                                :disabled="isInviteActionLoading(universityName, 'accept') || isInviteActionLoading(universityName, 'reject')"
                            >
                                {{ isInviteActionLoading(universityName, 'accept') ? 'Accepting...' : 'Accept invite' }}
                            </button>

                            <button
                                class="secondary-button invite-action-button invite-action-button--reject"
                                type="button"
                                @click="rejectInvite(universityName)"
                                :disabled="isInviteActionLoading(universityName, 'accept') || isInviteActionLoading(universityName, 'reject')"
                            >
                                {{ isInviteActionLoading(universityName, 'reject') ? 'Rejecting...' : 'Reject invite' }}
                            </button>
                        </div>
                    </article>
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

.invite-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.invite-card {
    border: 1px solid var(--ttl-border-subtle);
    border-radius: 0.95rem;
    background: rgba(143, 44, 226, 0.05);
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.invite-card__title {
    margin: 0;
    font-size: 1rem;
    font-weight: 800;
    color: var(--ttl-text-primary);
}

.invite-card__copy {
    margin: 0.2rem 0 0;
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
}

.invite-actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.6rem;
}

.invite-action-button {
    min-width: 9.5rem;
}

.invite-action-button--accept {
    background: linear-gradient(135deg, var(--ttl-accent) 0%, var(--ttl-accent-bright) 100%);
}

.invite-action-button--reject {
    border-color: color-mix(in srgb, var(--ttl-danger) 35%, transparent);
    color: var(--ttl-danger);
}

.invite-action-button--reject:hover:not(:disabled) {
    background: color-mix(in srgb, var(--ttl-danger) 8%, transparent);
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
    .invite-actions {
        grid-template-columns: 1fr;
    }

    .invite-actions {
        display: grid;
    }
}
</style>
