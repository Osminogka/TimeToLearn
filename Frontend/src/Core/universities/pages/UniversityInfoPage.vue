<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { isTeacherRole, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const students = ref([]);
const teachers = ref([]);
const isLoading = ref(false);
const isLoadingMembers = ref(false);
const isProcessingManagerAction = ref(false);
const isLeavingUniversity = ref(false);
const isJoining = ref(false);
const isMember = ref(false);
const isManager = ref(false);
const teacherSearchInput = ref('');
const teacherSearchTerm = ref('');
const studentSearchInput = ref('');
const studentSearchTerm = ref('');
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
    teacherEmail: '',
    studentEmail: '',
});

const leaveFlow = reactive({
    isConfirmationOpen: false,
    universityNameCheck: '',
    dangerAcknowledged: false,
});

const universityName = computed(() => String(route.params.name || ''));
const isTeacher = computed(() => isTeacherRole(user.value.role));

const joinLabel = computed(() => isTeacher.value ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);
const canLeaveUniversity = computed(() => isMember.value || isManager.value);
const leaveActionLabel = computed(() => {
    return isManager.value ? 'Delete university and leave' : 'Leave university';
});
const leaveWarningText = computed(() => {
    return isManager.value
        ? 'You are this university manager. Leaving will permanently delete the whole university and remove all members.'
        : 'Leaving will remove your membership from this university.';
});
const leaveConfirmationLabel = computed(() => {
    return isManager.value ? 'DELETE' : 'LEAVE';
});
const isLeaveConfirmationValid = computed(() => {
    return leaveFlow.dangerAcknowledged
        && leaveFlow.universityNameCheck.trim().toLowerCase() === universityName.value.trim().toLowerCase();
});
const directorName = computed(() => {
    return university.value?.directorUsername
        || university.value?.DirectorUsername
        || (university.value?.directorId || university.value?.DirectorId
            ? `Director #${university.value?.directorId || university.value?.DirectorId}`
            : 'Director information unavailable');
});

const filteredTeachers = computed(() => {
    return teachers.value.filter((name) => isNicknameMatch(name, teacherSearchTerm.value));
});

const filteredStudents = computed(() => {
    return students.value.filter((name) => isNicknameMatch(name, studentSearchTerm.value));
});

function levenshteinDistance(left, right) {
    const source = String(left || '').toLowerCase();
    const target = String(right || '').toLowerCase();

    if (!source.length) {
        return target.length;
    }
    if (!target.length) {
        return source.length;
    }

    const matrix = Array.from({ length: source.length + 1 }, () => Array(target.length + 1).fill(0));

    for (let i = 0; i <= source.length; i += 1) {
        matrix[i][0] = i;
    }

    for (let j = 0; j <= target.length; j += 1) {
        matrix[0][j] = j;
    }

    for (let i = 1; i <= source.length; i += 1) {
        for (let j = 1; j <= target.length; j += 1) {
            const substitutionCost = source[i - 1] === target[j - 1] ? 0 : 1;
            matrix[i][j] = Math.min(
                matrix[i - 1][j] + 1,
                matrix[i][j - 1] + 1,
                matrix[i - 1][j - 1] + substitutionCost,
            );
        }
    }

    return matrix[source.length][target.length];
}

function isNicknameMatch(nickname, searchTerm) {
    const normalizedNickname = String(nickname || '').trim().toLowerCase();
    const normalizedSearch = String(searchTerm || '').trim().toLowerCase();

    if (!normalizedSearch) {
        return true;
    }

    if (normalizedNickname.includes(normalizedSearch)) {
        return true;
    }

    const distance = levenshteinDistance(normalizedNickname, normalizedSearch);
    const allowedDistance = Math.max(1, Math.floor(normalizedSearch.length * 0.4));
    return distance <= allowedDistance;
}

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

function openLeaveConfirmation() {
    if (!canLeaveUniversity.value || isLeavingUniversity.value) {
        return;
    }

    clearMessages();
    leaveFlow.isConfirmationOpen = true;
    leaveFlow.universityNameCheck = '';
    leaveFlow.dangerAcknowledged = false;
}

function cancelLeaveConfirmation() {
    leaveFlow.isConfirmationOpen = false;
    leaveFlow.universityNameCheck = '';
    leaveFlow.dangerAcknowledged = false;
}

async function loadUniversity() {
    isLoading.value = true;
    clearMessages();

    try {
        authUtils.getCurrentUser();

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

        const currentUsername = String(user.value.name || '').toLowerCase();
        const directorUsername = String(university.value?.directorUsername || university.value?.DirectorUsername || '').toLowerCase();
        const currentUserId = Number(user.value.id || user.value.Id || 0);
        const directorId = Number(university.value?.directorId || university.value?.DirectorId || 0);
        isManager.value = (!!currentUsername && !!directorUsername && currentUsername === directorUsername)
            || (currentUserId > 0 && directorId > 0 && currentUserId === directorId);

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
            universityApi.getUniversityTeachers(universityName.value, { page: 1, pageSize: 100 }),
            universityApi.getUniversityStudents(universityName.value, { page: 1, pageSize: 100 }),
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
    if (!isManager.value || !inviteForm.teacherEmail.trim()) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.inviteTeacher(universityName.value, inviteForm.teacherEmail.trim());
        actionMessage.value = response?.message || response?.Message || 'Teacher invite sent.';
        inviteForm.teacherEmail = '';
    } catch (error) {
        errorMessage.value = error?.message || 'Could not invite teacher.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

async function inviteStudent() {
    if (!isManager.value || !inviteForm.studentEmail.trim()) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.inviteStudent(universityName.value, inviteForm.studentEmail.trim());
        actionMessage.value = response?.message || response?.Message || 'Student invite sent.';
        inviteForm.studentEmail = '';
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
        const country = manageForm.address.country.trim();
        const city = manageForm.address.city.trim();
        const street = manageForm.address.street.trim();
        const address = country || city || street ? { country, city, street } : null;

        const response = await universityApi.updateUniversityInfo({
            name: universityName.value,
            description: manageForm.description.trim(),
            isOpened: manageForm.isOpened,
            address,
        });

        actionMessage.value = response?.message || response?.Message || 'University info updated.';
        await refreshManagementData();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not update university settings.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

async function kickMember(username) {
    if (!isManager.value || !username) {
        return;
    }

    isProcessingManagerAction.value = true;
    clearMessages();

    try {
        const response = await universityApi.removeMember(universityName.value, username);
        actionMessage.value = response?.message || response?.Message || 'Member removed from university.';
        await loadMembers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not remove selected member.';
    } finally {
        isProcessingManagerAction.value = false;
    }
}

async function leaveUniversity() {
    if (!canLeaveUniversity.value || !isLeaveConfirmationValid.value || isLeavingUniversity.value) {
        return;
    }

    isLeavingUniversity.value = true;
    clearMessages();

    try {
        const response = await universityApi.leaveUniversity(universityName.value);
        const serverMessage = response?.message || response?.Message;
        actionMessage.value = serverMessage
            || (isManager.value
                ? 'University deleted and you left successfully.'
                : 'You left the university successfully.');

        cancelLeaveConfirmation();
        await router.push({ name: 'UniversitiesMine' });
    } catch (error) {
        errorMessage.value = error?.message || 'Could not leave the university.';
    } finally {
        isLeavingUniversity.value = false;
    }
}

function searchTeachers() {
    teacherSearchTerm.value = teacherSearchInput.value.trim();
}

function clearTeacherSearch() {
    teacherSearchInput.value = '';
    teacherSearchTerm.value = '';
}

function searchStudents() {
    studentSearchTerm.value = studentSearchInput.value.trim();
}

function clearStudentSearch() {
    studentSearchInput.value = '';
    studentSearchTerm.value = '';
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
                    </div>
                </div>
            </article>

            <div class="members-grid">
                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Teachers</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : filteredTeachers.length }}</span>
                    </div>

                    <div v-if="isManager" class="member-search-row">
                        <input
                            v-model="teacherSearchInput"
                            class="input-field"
                            type="text"
                            placeholder="Search teacher nickname"
                        />
                        <button class="secondary-button" type="button" @click="searchTeachers">
                            Search
                        </button>
                        <button class="secondary-button" type="button" @click="clearTeacherSearch" :disabled="!teacherSearchInput && !teacherSearchTerm">
                            Clear
                        </button>
                    </div>

                    <p v-if="!filteredTeachers.length" class="member-empty">No teachers found for this view.</p>

                    <ul v-else class="member-list">
                        <li v-for="teacherName in filteredTeachers" :key="teacherName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ teacherName }}</span>
                            <button
                                v-if="isManager"
                                class="secondary-button member-action"
                                type="button"
                                :disabled="isProcessingManagerAction"
                                @click="kickMember(teacherName)"
                            >
                                Kick
                            </button>
                        </li>
                    </ul>
                </article>

                <article class="surface-card member-block">
                    <div class="member-block__top">
                        <h3>Students</h3>
                        <span class="pill">{{ isLoadingMembers ? 'Loading...' : filteredStudents.length }}</span>
                    </div>

                    <div v-if="isManager" class="member-search-row">
                        <input
                            v-model="studentSearchInput"
                            class="input-field"
                            type="text"
                            placeholder="Search student nickname"
                        />
                        <button class="secondary-button" type="button" @click="searchStudents">
                            Search
                        </button>
                        <button class="secondary-button" type="button" @click="clearStudentSearch" :disabled="!studentSearchInput && !studentSearchTerm">
                            Clear
                        </button>
                    </div>

                    <p v-if="!filteredStudents.length" class="member-empty">No students found for this view.</p>

                    <ul v-else class="member-list">
                        <li v-for="studentName in filteredStudents" :key="studentName" class="member-item">
                            <AppIcon name="profile" />
                            <span>{{ studentName }}</span>
                            <button
                                v-if="isManager"
                                class="secondary-button member-action"
                                type="button"
                                :disabled="isProcessingManagerAction"
                                @click="kickMember(studentName)"
                            >
                                Kick
                            </button>
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
                            <select v-model="manageForm.isOpened" class="input-field">
                                <option :value="true">Open</option>
                                <option :value="false">Private</option>
                            </select>
                        </div>

                        <button class="submit-button" type="button" @click="saveUniversitySettings" :disabled="isProcessingManagerAction">
                            {{ isProcessingManagerAction ? 'Saving...' : 'Save university settings' }}
                        </button>
                    </article>

                    <article class="manager-card">
                        <h4>Invite members</h4>

                        <div class="field-group">
                            <label class="field-label" for="invite-teacher">Invite verified teacher by email</label>
                            <input
                                id="invite-teacher"
                                v-model="inviteForm.teacherEmail"
                                class="input-field"
                                type="email"
                                placeholder="teacher@email.com"
                            />
                            <button class="secondary-button" type="button" @click="inviteTeacher" :disabled="!inviteForm.teacherEmail.trim() || isProcessingManagerAction">
                                Invite teacher
                            </button>
                        </div>

                        <div class="field-group">
                            <label class="field-label" for="invite-student">Invite student by email</label>
                            <input
                                id="invite-student"
                                v-model="inviteForm.studentEmail"
                                class="input-field"
                                type="email"
                                placeholder="student@email.com"
                            />
                            <button class="secondary-button" type="button" @click="inviteStudent" :disabled="!inviteForm.studentEmail.trim() || isProcessingManagerAction">
                                Invite student
                            </button>
                        </div>
                    </article>
                </div>
            </section>

            <section v-if="canLeaveUniversity" class="surface-card leave-panel">
                <div class="leave-panel__top">
                    <div>
                        <p class="section-kicker">Membership action</p>
                        <h3>Leave university</h3>
                    </div>
                    <span class="pill pill--pink">Destructive action</span>
                </div>

                <p class="leave-panel__warning">
                    {{ leaveWarningText }}
                </p>

                <button
                    class="secondary-button leave-panel__trigger"
                    type="button"
                    :disabled="isLeavingUniversity || isProcessingManagerAction"
                    @click="openLeaveConfirmation"
                >
                    {{ leaveActionLabel }}
                </button>

                <div v-if="leaveFlow.isConfirmationOpen" class="surface-card leave-confirmation">
                    <p>
                        Confirm this action by typing
                        <strong>{{ universityName }}</strong>
                        below.
                    </p>

                    <input
                        v-model="leaveFlow.universityNameCheck"
                        class="input-field"
                        type="text"
                        :placeholder="`Type ${universityName}`"
                    />

                    <label class="leave-confirmation__checkbox">
                        <input v-model="leaveFlow.dangerAcknowledged" type="checkbox" />
                        <span>I understand this action cannot be undone.</span>
                    </label>

                    <div class="leave-confirmation__actions">
                        <button class="secondary-button" type="button" @click="cancelLeaveConfirmation" :disabled="isLeavingUniversity">
                            Cancel
                        </button>
                        <button
                            class="submit-button submit-button--danger"
                            type="button"
                            :disabled="!isLeaveConfirmationValid || isLeavingUniversity"
                            @click="leaveUniversity"
                        >
                            {{ isLeavingUniversity ? 'Processing...' : `${leaveConfirmationLabel} NOW` }}
                        </button>
                    </div>
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
    max-height: 18rem;
    overflow-y: auto;
    padding-right: 0.25rem;
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

.member-action {
    margin-left: auto;
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

.manager-textarea {
    min-height: 6.4rem;
    resize: vertical;
}

.manager-address-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 0.65rem;
}

.member-search-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    gap: 0.6rem;
    align-items: center;
}

.leave-panel {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    border: 1px solid rgba(212, 61, 106, 0.24);
    background: linear-gradient(180deg, rgba(255, 232, 239, 0.55), rgba(255, 255, 255, 0.9));
}

.leave-panel__top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
}

.leave-panel__top h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.leave-panel__warning {
    margin: 0;
    color: var(--ttl-text-secondary);
}

.leave-panel__trigger {
    align-self: flex-start;
}

.leave-confirmation {
    padding: 0.9rem;
    border: 1px dashed rgba(212, 61, 106, 0.44);
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.leave-confirmation p {
    margin: 0;
    color: var(--ttl-text-primary);
}

.leave-confirmation__checkbox {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--ttl-text-secondary);
}

.leave-confirmation__actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.55rem;
}

.submit-button--danger {
    background: linear-gradient(135deg, #d43d6a 0%, #bf2a56 100%);
    box-shadow: 0 10px 20px rgba(212, 61, 106, 0.25);
}

.submit-button--danger:hover {
    transform: translateY(-1px);
    box-shadow: 0 12px 24px rgba(212, 61, 106, 0.3);
}

@media (max-width: 900px) {
    .members-grid {
        grid-template-columns: 1fr;
    }

    .manager-grid {
        grid-template-columns: 1fr;
    }

    .member-search-row {
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
    .member-search-row {
        grid-template-columns: 1fr;
    }

    .leave-confirmation__actions {
        flex-direction: column;
        align-items: stretch;
    }
}
</style>
