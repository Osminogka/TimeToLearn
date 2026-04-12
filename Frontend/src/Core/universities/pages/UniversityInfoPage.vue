<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';
import userApi from '@/Users/services/userApi';

const route = useRoute();

const university = ref(null);
const students = ref([]);
const teachers = ref([]);
const allUsers = ref([]);
const selectedMember = ref('');
const currentUserId = ref(0);
const isLoading = ref(false);
const isLoadingMembers = ref(false);
const isProcessingManagerAction = ref(false);
const isJoining = ref(false);
const isMember = ref(false);
const isManager = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const manageForm = reactive({
    description: '',
    isOpened: true,
    address: {
        country: '',
        city: '',
        street: '',
    },
});

const inviteForm = reactive({
    teacherUsername: '',
    studentUsername: '',
});

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));

const joinLabel = computed(() => isTeacher.value ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);
const directorName = computed(() => {
    return university.value?.directorUsername
        || university.value?.DirectorUsername
        || (university.value?.directorId || university.value?.DirectorId
            ? `Director #${university.value?.directorId || university.value?.DirectorId}`
            : 'Director information unavailable');
});

const teacherOptions = computed(() => {
    const existing = new Set(teachers.value.map((name) => String(name).toLowerCase()));
    const currentUsername = String(user.value.name || '').toLowerCase();

    return allUsers.value.filter((name) => {
        const normalized = String(name).toLowerCase();
        return normalized !== currentUsername && !existing.has(normalized);
    });
});

const studentOptions = computed(() => {
    const existing = new Set(students.value.map((name) => String(name).toLowerCase()));
    const currentUsername = String(user.value.name || '').toLowerCase();

    return allUsers.value.filter((name) => {
        const normalized = String(name).toLowerCase();
        return normalized !== currentUsername && !existing.has(normalized);
    });
});

const allMembers = computed(() => {
    return [
        ...teachers.value.map((name) => ({ username: name, role: 'Teacher' })),
        ...students.value.map((name) => ({ username: name, role: 'Student' })),
    ];
});

function normalizeValue(payload) {
    return payload.value || payload.Value || null;
}

function normalizeItems(payload) {
    return payload.items || payload.Items || payload.enum || payload.Enum || [];
}

function setManageFormFromUniversity() {
    manageForm.description = university.value?.description || '';
    manageForm.isOpened = Boolean(university.value?.isOpened);
    manageForm.address.country = university.value?.address?.country || '';
    manageForm.address.city = university.value?.address?.city || '';
    manageForm.address.street = university.value?.address?.street || '';
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

async function loadCurrentUserContext() {
    const currentName = String(user.value.name || '').trim();
    if (!currentName) {
        currentUserId.value = 0;
        return;
    }

    const profileResponse = await userApi.getByName(currentName);
    const profile = normalizeValue(profileResponse);
    currentUserId.value = Number(profile?.id || profile?.Id || 0);
}

async function loadAllUsers() {
    if (!isManager.value) {
        allUsers.value = [];
        return;
    }

    const usersResponse = await userApi.getAllUsers();
    allUsers.value = normalizeItems(usersResponse);
}

async function loadUniversity() {
    isLoading.value = true;
    clearMessages();

    try {
        authUtils.getCurrentUser();

        await loadCurrentUserContext();

        const [universityResponse, myUniversitiesResponse] = await Promise.all([
            universityApi.getUniversityByName(universityName.value),
            universityApi.getMyUniversities({ page: 1, pageSize: 100 }),
        ]);

        university.value = normalizeValue(universityResponse);

        const myItems = normalizeItems(myUniversitiesResponse);
        isMember.value = myItems.some((item) => {
            const currentName = String(item.name || item.Name || '').toLowerCase();
            return currentName === universityName.value.toLowerCase();
        });

        isManager.value = Boolean(university.value?.directorId) && Number(university.value.directorId) === currentUserId.value;

        setManageFormFromUniversity();
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load university information.';
        university.value = null;
        isMember.value = false;
        isManager.value = false;
    } finally {
        isLoading.value = false;
    }
}

async function loadMembers() {
    if (!universityName.value) {
        return;
    }

    isLoadingMembers.value = true;

    try {
        const [teachersResponse, studentsResponse] = await Promise.all([
            universityApi.getUniversityTeachers(universityName.value, { page: 1, pageSize: 8 }),
            universityApi.getUniversityStudents(universityName.value, { page: 1, pageSize: 8 }),
        ]);

        teachers.value = normalizeItems(teachersResponse);
        students.value = normalizeItems(studentsResponse);
    } catch {
        teachers.value = [];
        students.value = [];
    } finally {
        isLoadingMembers.value = false;
    }
}

async function refreshManagementData() {
    await loadUniversity();
    await loadMembers();
    await loadAllUsers();
}

async function joinUniversity() {
    if (!university.value?.isOpened || isMember.value) {
        return;
    }

    isJoining.value = true;
    actionMessage.value = '';
    errorMessage.value = '';

    try {
        const response = await universityApi.enterUniversity(universityName.value);
        const serverMessage = response?.message || response?.Message;
        actionMessage.value = serverMessage || 'You entered the university as student.';
        isMember.value = true;
        await loadMembers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

async function inviteTeacher() {
    if (!isManager.value || !inviteForm.teacherUsername) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.inviteTeacher(universityName.value, inviteForm.teacherUsername);
        actionMessage.value = response?.message || response?.Message || 'Teacher invite sent.';
        inviteForm.teacherUsername = '';
    } catch (error) {
        errorMessage.value = error?.message || 'Could not invite teacher.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

async function inviteStudent() {
    if (!isManager.value || !inviteForm.studentUsername) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.inviteStudent(universityName.value, inviteForm.studentUsername);
        actionMessage.value = response?.message || response?.Message || 'Student invite sent.';
        inviteForm.studentUsername = '';
    } catch (error) {
        errorMessage.value = error?.message || 'Could not invite student.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

async function saveUniversitySettings() {
    if (!isManager.value) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.updateUniversityInfo({
            name: universityName.value,
            description: manageForm.description.trim(),
            isOpened: manageForm.isOpened,
            address: {
                country: manageForm.address.country.trim(),
                city: manageForm.address.city.trim(),
                street: manageForm.address.street.trim(),
            },
        });

        actionMessage.value = response?.message || response?.Message || 'University info updated.';
        await refreshManagementData();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not update university settings.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

function selectMember(username) {
    selectedMember.value = selectedMember.value === username ? '' : username;
}

async function kickSelectedMember() {
    if (!isManager.value || !selectedMember.value) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.removeMember(universityName.value, selectedMember.value);
        actionMessage.value = response?.message || response?.Message || 'Member removed from university.';
        selectedMember.value = '';
        await loadMembers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not remove selected member.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

onMounted(async () => {
    await refreshManagementData();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="info"
            subtitle="Overview of university details, access setup, and members."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoading"
            @join="joinUniversity"
        />

        <section class="surface-card page-content">
            <div class="page-content__heading-row">
                <div>
                    <p class="section-kicker">University profile</p>
                    <h2 class="section-title">Information</h2>
                </div>

                <button class="secondary-button" type="button" @click="loadUniversity" :disabled="isLoading">
                    {{ isLoading ? 'Refreshing...' : 'Refresh info' }}
                </button>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="surface-card info-card">
                <div class="info-card__top">
                    <h3>University details</h3>
                    <span class="pill" :class="university?.isOpened ? 'pill--accent' : 'pill--pink'">
                        {{ university?.isOpened ? 'Open access' : 'Private access' }}
                    </span>
                </div>

                <p class="section-copy">{{ university?.description || 'Description will appear here.' }}</p>

                <div class="address-line">
                    <AppIcon name="info" />
                    <p>
                        {{ university?.address?.country || 'Country N/A' }}
                        •
                        {{ university?.address?.city || 'City N/A' }}
                        •
                        {{ university?.address?.street || 'Street N/A' }}
                    </p>
                </div>
            </article>

            <article class="surface-card director-card">
                <div class="director-card__top">
                    <h3>University director</h3>
                    <span class="pill pill--accent">Manager</span>
                </div>

                <div class="director-card__person">
                    <AppIcon name="profile" />
                    <div>
                        <p class="director-card__name">{{ directorName }}</p>
                        <p class="director-card__caption">Responsible for invites, membership, and university settings.</p>
                    </div>
                </div>
            </article>

            <div class="members-grid">
                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Teachers</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : teachers.length }}</span>
                    </div>

                    <p v-if="!teachers.length" class="member-empty">No teachers available or access is limited.</p>

                    <ul v-else class="member-list">
                        <li v-for="teacherName in teachers" :key="teacherName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ teacherName }}</span>
                        </li>
                    </ul>
                </article>

                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Students</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : students.length }}</span>
                    </div>

                    <p v-if="!students.length" class="member-empty">No students available or access is limited.</p>

                    <ul v-else class="member-list">
                        <li v-for="studentName in students" :key="studentName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ studentName }}</span>
                        </li>
                    </ul>
                </article>
            </div>

            <section v-if="isManager" class="surface-card manager-panel">
                <div class="manager-panel__top">
                    <div>
                        <p class="section-kicker">Manager controls</p>
                        <h3>University administration</h3>
                    </div>
                    <span class="pill pill--accent">Creator access</span>
                </div>

                <div class="manager-grid">
                    <article class="manager-card">
                        <h4>Update university info</h4>

                        <div class="field-group">
                            <label class="field-label" for="university-description">Description</label>
                            <textarea
                                id="university-description"
                                v-model="manageForm.description"
                                class="input-field manager-textarea"
                                maxlength="500"
                            />
                        </div>

                        <div class="field-group">
                            <label class="field-label">Address</label>
                            <div class="manager-address-grid">
                                <input v-model="manageForm.address.country" class="input-field" type="text" placeholder="Country" />
                                <input v-model="manageForm.address.city" class="input-field" type="text" placeholder="City" />
                                <input v-model="manageForm.address.street" class="input-field" type="text" placeholder="Street" />
                            </div>
                        </div>

                        <div class="field-group">
                            <label class="field-label">Access type</label>
                            <div class="toggle-row">
                                <button class="secondary-button toggle-row__item" :class="{ 'toggle-row__item--active': manageForm.isOpened }" type="button" @click="manageForm.isOpened = true">
                                    Open
                                </button>
                                <button class="secondary-button toggle-row__item" :class="{ 'toggle-row__item--active': !manageForm.isOpened }" type="button" @click="manageForm.isOpened = false">
                                    Private
                                </button>
                            </div>
                        </div>

                        <button class="submit-button" type="button" @click="saveUniversitySettings" :disabled="isProcessingManagerAction">
                            {{ isProcessingManagerAction ? 'Saving...' : 'Save university settings' }}
                        </button>
                    </article>

                    <article class="manager-card">
                        <h4>Invite members</h4>

                        <div class="field-group">
                            <label class="field-label" for="invite-teacher">Invite teacher</label>
                            <select id="invite-teacher" v-model="inviteForm.teacherUsername" class="input-field">
                                <option value="">Select teacher username</option>
                                <option v-for="teacherName in teacherOptions" :key="`invite-teacher-${teacherName}`" :value="teacherName">{{ teacherName }}</option>
                            </select>
                            <button class="secondary-button" type="button" @click="inviteTeacher" :disabled="!inviteForm.teacherUsername || isProcessingManagerAction">
                                Invite teacher
                            </button>
                        </div>

                        <div class="field-group">
                            <label class="field-label" for="invite-student">Invite student</label>
                            <select id="invite-student" v-model="inviteForm.studentUsername" class="input-field">
                                <option value="">Select student username</option>
                                <option v-for="studentName in studentOptions" :key="`invite-student-${studentName}`" :value="studentName">{{ studentName }}</option>
                            </select>
                            <button class="secondary-button" type="button" @click="inviteStudent" :disabled="!inviteForm.studentUsername || isProcessingManagerAction">
                                Invite student
                            </button>
                        </div>
                    </article>

                    <article class="manager-card manager-card--wide">
                        <h4>Members management</h4>

                        <p class="section-copy">Select any member below to remove them from this university.</p>

                        <div class="manager-members-list">
                            <button
                                v-for="member in allMembers"
                                :key="`member-${member.role}-${member.username}`"
                                class="manager-member"
                                :class="{ 'manager-member--active': selectedMember === member.username }"
                                type="button"
                                @click="selectMember(member.username)"
                            >
                                <span>{{ member.username }}</span>
                                <span class="pill">{{ member.role }}</span>
                            </button>
                        </div>

                        <button class="submit-button manager-kick" type="button" @click="kickSelectedMember" :disabled="!selectedMember || isProcessingManagerAction">
                            {{ isProcessingManagerAction ? 'Processing...' : selectedMember ? `Kick ${selectedMember}` : 'Select member to kick' }}
                        </button>
                    </article>
                </div>
            </section>
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

.page-content {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.page-content__heading-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.8rem;
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

.info-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.info-card__top {
    display: flex;
    justify-content: space-between;
    gap: 0.7rem;
    align-items: center;
}

.info-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.address-line {
    display: flex;
    align-items: center;
    gap: 0.65rem;
}

.address-line p {
    margin: 0;
    color: var(--ttl-text-muted);
}

.director-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.director-card__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.director-card__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.director-card__person {
    display: flex;
    align-items: center;
    gap: 0.7rem;
}

.director-card__name {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 800;
}

.director-card__caption {
    margin: 0.2rem 0 0;
    color: var(--ttl-text-secondary);
    font-size: 0.88rem;
}

.members-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.85rem;
}

.member-block {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.member-block__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.member-block__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.member-empty {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.member-list {
    margin: 0;
    padding: 0;
    list-style: none;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.member-item {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    padding: 0.5rem 0.6rem;
    border-radius: 0.8rem;
    background: rgba(143, 44, 226, 0.06);
    color: var(--ttl-text-primary);
}

.manager-panel {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.manager-panel__top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.8rem;
}

.manager-panel__top h3,
.manager-card h4 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.manager-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.85rem;
}

.manager-card {
    padding: 1rem;
    border-radius: 1rem;
    border: 1px solid var(--ttl-border-subtle);
    background: rgba(255, 255, 255, 0.72);
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.manager-card--wide {
    grid-column: 1 / -1;
}

.manager-textarea {
    min-height: 6.4rem;
    resize: vertical;
}

.manager-address-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.65rem;
}

.toggle-row {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.65rem;
}

.toggle-row__item--active {
    background: rgba(143, 44, 226, 0.18);
}

.manager-members-list {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.6rem;
}

.manager-member {
    border: 1px solid var(--ttl-border-subtle);
    border-radius: 0.8rem;
    background: rgba(143, 44, 226, 0.06);
    color: var(--ttl-text-primary);
    min-height: 2.75rem;
    padding: 0.5rem 0.7rem;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.6rem;
    cursor: pointer;
    transition: border-color var(--ttl-transition-base), background var(--ttl-transition-base), transform var(--ttl-transition-fast);
}

.manager-member:hover {
    transform: translateY(-1px);
}

.manager-member--active {
    border-color: rgba(143, 44, 226, 0.35);
    background: rgba(143, 44, 226, 0.14);
}

.manager-kick {
    width: 100%;
}

@media (max-width: 900px) {
    .members-grid {
        grid-template-columns: 1fr;
    }

    .manager-grid {
        grid-template-columns: 1fr;
    }

    .manager-members-list {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .page-content__heading-row,
    .info-card__top,
    .director-card__top,
    .manager-panel__top {
        flex-direction: column;
        align-items: flex-start;
    }

    .manager-address-grid,
    .toggle-row {
        grid-template-columns: 1fr;
    }
}
</style>
