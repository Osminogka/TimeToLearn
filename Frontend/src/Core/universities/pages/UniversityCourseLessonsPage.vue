<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import AppIcon from '@/Shared/components/AppIcon.vue';
import courseApi from '@/Courses/services/courseApi';
import lessonResourcesService from '@/Courses/services/lessonResourcesService';
import markdownService from '@/Courses/services/markdownService';
import LessonCard from '@/Courses/components/LessonCard.vue';
import LessonFormPanel from '@/Courses/components/LessonFormPanel.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const course = ref(null);
const lessons = ref([]);

const isLoadingWorkspace = ref(false);
const isLoadingLessons = ref(false);
const isSubmittingLesson = ref(false);
const isJoining = ref(false);

const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');
const formErrorMessage = ref('');

const isLessonFormOpen = ref(false);
const editingLessonId = ref(0);
const selectedLessonId = ref(0);
const selectedLesson = ref(null);
const isCompletingLesson = ref(false);
const isSavingGrade = ref(false);
const courseProgress = ref(null);
const studentsProgress = ref([]);
const gradeDraftByStudent = ref({});
const selectedTeacherStudentId = ref(0);

const isLoadingLessonQuiz = ref(false);
const isLoadingCourseQuiz = ref(false);
const isLoadingStudentQuizAnswers = ref(false);
const isLoadingTeacherQuizAnswers = ref(false);
const isSavingCourseQuiz = ref(false);
const isSubmittingLessonQuiz = ref(false);
const isSubmittingCourseQuiz = ref(false);

const lessonQuizQuestions = ref([]);
const courseQuizQuestions = ref([]);
const myLessonQuizAnswers = ref([]);
const myCourseQuizAnswers = ref([]);
const teacherLessonQuizAnswers = ref([]);
const teacherCourseQuizAnswers = ref([]);

const lessonQuizAnswerDraftByQuestion = ref({});
const courseQuizAnswerDraftByQuestion = ref({});
const editingLessonQuizQuestions = ref([]);
const editingLessonQuizAttemptPolicy = ref('reattempt');
const isCourseQuizEditorOpen = ref(false);
const courseQuizEditorQuestions = ref([]);
const courseQuizEditorAttemptPolicy = ref('reattempt');

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));
const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const canView = computed(() => isMember.value);

const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const hasLessons = computed(() => lessons.value.length > 0);
const hasLessonQuizQuestions = computed(() => lessonQuizQuestions.value.length > 0);
const hasCourseQuizQuestions = computed(() => courseQuizQuestions.value.length > 0);
const isEditingMode = computed(() => Boolean(editingLessonId.value));
const progressCompletedCount = computed(() => Number(courseProgress.value?.completedLessons || courseProgress.value?.CompletedLessons || 0));
const progressTotalCount = computed(() => Number(courseProgress.value?.totalLessons || courseProgress.value?.TotalLessons || lessons.value.length));
const progressPercent = computed(() => {
    if (!progressTotalCount.value) {
        return 0;
    }

    const rawPercent = Number(courseProgress.value?.percentComplete || courseProgress.value?.PercentComplete || 0);
    return Number.isFinite(rawPercent)
        ? Math.max(0, Math.min(100, rawPercent))
        : Math.round((progressCompletedCount.value / progressTotalCount.value) * 100);
});

const studentMark = computed(() => {
    const raw = Number(courseProgress.value?.mark || courseProgress.value?.Mark);
    return Number.isInteger(raw) && raw >= 1 && raw <= 10 ? raw : null;
});

const myLessonQuizCorrectCount = computed(() => myLessonQuizAnswers.value.filter(item => item.isCorrect || item.IsCorrect).length);
const myCourseQuizCorrectCount = computed(() => myCourseQuizAnswers.value.filter(item => item.isCorrect || item.IsCorrect).length);

const myLessonQuizScoreLabel = computed(() => `${myLessonQuizCorrectCount.value}/${myLessonQuizAnswers.value.length}`);
const myCourseQuizScoreLabel = computed(() => `${myCourseQuizCorrectCount.value}/${myCourseQuizAnswers.value.length}`);

const selectedTeacherStudent = computed(() => {
    if (!canManage.value || !studentsProgress.value.length) {
        return null;
    }

    const selected = studentsProgress.value.find((item) => getStudentId(item) === selectedTeacherStudentId.value);
    return selected || studentsProgress.value[0] || null;
});

const selectedTeacherStudentLessonQuizAnswers = computed(() => {
    const studentId = getStudentId(selectedTeacherStudent.value);
    if (!studentId) {
        return [];
    }

    return teacherLessonQuizAnswers.value.filter((item) => Number(item?.studentId || item?.StudentId || 0) === studentId);
});

const selectedTeacherStudentCourseQuizAnswers = computed(() => {
    const studentId = getStudentId(selectedTeacherStudent.value);
    if (!studentId) {
        return [];
    }

    return teacherCourseQuizAnswers.value.filter((item) => Number(item?.studentId || item?.StudentId || 0) === studentId);
});

const selectedTeacherStudentCanBeMarked = computed(() => {
    const student = selectedTeacherStudent.value;
    if (!student) {
        return false;
    }

    return getStudentCanBeMarked(student);
});

const selectedLessonCompleted = computed(() => {
    const raw = selectedLessonResolved.value?.isCompletedByCurrentUser ?? selectedLessonResolved.value?.IsCompletedByCurrentUser;
    return Boolean(raw);
});

const canCompleteSelectedLesson = computed(() => {
    if (!selectedLessonResolved.value || canManage.value || isCompletingLesson.value) {
        return false;
    }

    return !selectedLessonCompleted.value;
});

const editingLesson = computed(() => {
    if (!editingLessonId.value) {
        return null;
    }

    return lessons.value.find((item) => Number(item.id || item.Id) === editingLessonId.value) || null;
});

const selectedLessonResolved = computed(() => {
    if (selectedLesson.value) {
        return selectedLesson.value;
    }

    if (!selectedLessonId.value) {
        return null;
    }

    return lessons.value.find((item) => Number(item.id || item.Id) === selectedLessonId.value) || null;
});

const selectedLessonResources = computed(() => lessonResourcesService.getLessonResources(selectedLessonResolved.value));

const editingLessonResources = computed(() => lessonResourcesService.getLessonResources(editingLesson.value));

const isSelectedLessonMarkdown = computed(() => {
    const raw = selectedLessonResolved.value?.isMarkdown ?? selectedLessonResolved.value?.IsMarkdown;
    return Boolean(raw);
});

function normalizeItems(payload) {
    return payload?.items || payload?.Items || payload?.values || payload?.Values || [];
}

function normalizeValue(payload) {
    return payload?.value || payload?.Value || null;
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function getLessonId(item) {
    return Number(item?.id || item?.Id || 0);
}

function getCourseTitle() {
    return course.value?.title || course.value?.Title || 'Course';
}

function normalizeStudentsProgress(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function getStudentId(item) {
    return Number(item?.studentId || item?.StudentId || 0);
}

function getStudentLessonsCompleted(item) {
    return Boolean(item?.isCompleted ?? item?.IsCompleted);
}

function getStudentQuizzesCompleted(item) {
    return Boolean(item?.areAllQuizzesCompleted ?? item?.AreAllQuizzesCompleted);
}

function getStudentCanBeMarked(item) {
    const explicit = item?.canBeMarked ?? item?.CanBeMarked;
    if (explicit !== undefined && explicit !== null) {
        return Boolean(explicit);
    }

    return getStudentLessonsCompleted(item) && getStudentQuizzesCompleted(item);
}

function getDraftMark(student) {
    const studentId = getStudentId(student);
    const rawFromDraft = gradeDraftByStudent.value[studentId];
    if (rawFromDraft !== undefined) {
        return Number(rawFromDraft) || 1;
    }

    const existingMark = Number(student?.mark || student?.Mark);
    return Number.isInteger(existingMark) && existingMark >= 1 && existingMark <= 10 ? existingMark : 1;
}

function normalizeQuestionRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function normalizeAnswerRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function getQuestionId(item) {
    return Number(item?.id || item?.Id || 0);
}

function getOptionId(item) {
    return Number(item?.id || item?.Id || 0);
}

function createEmptyQuestionDraft() {
    return {
        questionText: '',
        optionTexts: ['', ''],
        correctOptionIndex: 0,
    };
}

function mapQuestionToDraft(question) {
    const options = (question?.options || question?.Options || [])
        .map((item) => String(item?.optionText || item?.OptionText || '').trim())
        .filter((item) => item.length > 0);

    const correctOptionIndex = (question?.options || question?.Options || []).findIndex((item) => Boolean(item?.isCorrect ?? item?.IsCorrect));

    return {
        questionText: String(question?.questionText || question?.QuestionText || ''),
        optionTexts: options.length >= 2 ? options : ['', ''],
        correctOptionIndex: correctOptionIndex >= 0 ? correctOptionIndex : 0,
    };
}

function mapQuestionsToDraft(questions) {
    const rows = Array.isArray(questions) ? questions : [];
    if (!rows.length) {
        return [createEmptyQuestionDraft()];
    }

    return rows.map((item) => mapQuestionToDraft(item));
}

function resolveDraftCollection(target) {
    if (Array.isArray(target)) {
        return target;
    }

    return target?.value;
}

function buildQuizQuestionPayload(draft) {
    const questionText = String(draft?.questionText || '').trim();
    const optionTexts = Array.isArray(draft?.optionTexts) ? draft.optionTexts : [];
    const correctIndex = Number(draft?.correctOptionIndex || 0);

    const options = optionTexts
        .map((text, index) => ({
            optionText: String(text || '').trim(),
            isCorrect: index === correctIndex,
        }))
        .filter((opt) => opt.optionText.length > 0);

    return { questionText, options };
}

function normalizeAttemptPolicy(rawValue) {
    const raw = String(rawValue || 'reattempt').trim().toLowerCase();
    return raw === 'single' ? 'single' : 'reattempt';
}

function getQuizAttemptPolicy(questions) {
    const rows = Array.isArray(questions) ? questions : [];
    if (!rows.length) {
        return 'reattempt';
    }

    return normalizeAttemptPolicy(rows[0]?.attemptPolicy || rows[0]?.AttemptPolicy || 'reattempt');
}

function addQuestionDraftRow(targetRef) {
    const collection = resolveDraftCollection(targetRef);
    if (!Array.isArray(collection)) {
        return;
    }

    collection.push(createEmptyQuestionDraft());
}

function removeQuestionDraftRow(targetRef, questionIndex) {
    const collection = resolveDraftCollection(targetRef);
    if (!Array.isArray(collection) || collection.length <= 1) {
        return;
    }

    collection.splice(questionIndex, 1);
}

function addOptionToQuestion(targetRef, questionIndex) {
    const collection = resolveDraftCollection(targetRef);
    if (!Array.isArray(collection) || !collection[questionIndex]) {
        return;
    }

    collection[questionIndex].optionTexts.push('');
}

function removeOptionFromQuestion(targetRef, questionIndex, optionIndex) {
    const collection = resolveDraftCollection(targetRef);
    if (!Array.isArray(collection)) {
        return;
    }

    const question = collection[questionIndex];
    if (!question || question.optionTexts.length <= 2) {
        return;
    }

    question.optionTexts.splice(optionIndex, 1);
    if (question.correctOptionIndex >= question.optionTexts.length) {
        question.correctOptionIndex = 0;
    }
}

function isSingleAttemptLessonQuiz() {
    return getQuizAttemptPolicy(lessonQuizQuestions.value) === 'single';
}

function isSingleAttemptCourseQuiz() {
    return getQuizAttemptPolicy(courseQuizQuestions.value) === 'single';
}

function canEditLessonQuizAnswers() {
    if (canManage.value) {
        return false;
    }

    return !(isSingleAttemptLessonQuiz() && myLessonQuizAnswers.value.length > 0);
}

function canEditCourseQuizAnswers() {
    if (canManage.value) {
        return false;
    }

    return !(isSingleAttemptCourseQuiz() && myCourseQuizAnswers.value.length > 0);
}

function hasSelectedAnswersForAllQuestions(questions, drafts) {
    const rows = Array.isArray(questions) ? questions : [];
    if (!rows.length) {
        return false;
    }

    return rows.every((question) => {
        const questionId = getQuestionId(question);
        return questionId > 0 && Number(drafts?.[questionId] || 0) > 0;
    });
}

function buildSubmitAnswersPayload(questions, drafts) {
    return (Array.isArray(questions) ? questions : []).map((question) => {
        const questionId = getQuestionId(question);
        return {
            questionId,
            selectedOptionId: Number(drafts?.[questionId] || 0),
        };
    });
}

function getExistingAnswerOptionId(questionId, answers) {
    const row = answers.find(item => Number(item?.questionId || item?.QuestionId) === questionId);
    if (!row) {
        return 0;
    }

    return Number(row?.selectedOptionId || row?.SelectedOptionId || 0);
}

async function loadSelectedLessonQuiz() {
    if (!canView.value || !selectedLessonId.value) {
        lessonQuizQuestions.value = [];
        return;
    }

    isLoadingLessonQuiz.value = true;
    try {
        const response = await courseApi.getLessonQuizQuestions(selectedLessonId.value);
        lessonQuizQuestions.value = normalizeQuestionRows(response);
    } catch {
        lessonQuizQuestions.value = [];
    } finally {
        isLoadingLessonQuiz.value = false;
    }
}

async function loadCourseQuiz() {
    if (!canView.value || !courseId.value) {
        courseQuizQuestions.value = [];
        return;
    }

    isLoadingCourseQuiz.value = true;
    try {
        const response = await courseApi.getCourseQuizQuestions(courseId.value);
        courseQuizQuestions.value = normalizeQuestionRows(response);
    } catch {
        courseQuizQuestions.value = [];
    } finally {
        isLoadingCourseQuiz.value = false;
    }
}

async function loadMyQuizAnswers() {
    if (!canView.value || canManage.value) {
        myLessonQuizAnswers.value = [];
        myCourseQuizAnswers.value = [];
        lessonQuizAnswerDraftByQuestion.value = {};
        courseQuizAnswerDraftByQuestion.value = {};
        return;
    }

    isLoadingStudentQuizAnswers.value = true;
    try {
        const lessonResponse = selectedLessonId.value
            ? await courseApi.getMyLessonQuizAnswers(selectedLessonId.value)
            : { values: [] };
        const courseResponse = await courseApi.getMyCourseQuizAnswers(courseId.value);

        myLessonQuizAnswers.value = normalizeAnswerRows(lessonResponse);
        myCourseQuizAnswers.value = normalizeAnswerRows(courseResponse);

        lessonQuizAnswerDraftByQuestion.value = lessonQuizQuestions.value.reduce((acc, item) => {
            const questionId = getQuestionId(item);
            acc[questionId] = getExistingAnswerOptionId(questionId, myLessonQuizAnswers.value);
            return acc;
        }, {});

        courseQuizAnswerDraftByQuestion.value = courseQuizQuestions.value.reduce((acc, item) => {
            const questionId = getQuestionId(item);
            acc[questionId] = getExistingAnswerOptionId(questionId, myCourseQuizAnswers.value);
            return acc;
        }, {});
    } catch {
        myLessonQuizAnswers.value = [];
        myCourseQuizAnswers.value = [];
        lessonQuizAnswerDraftByQuestion.value = {};
        courseQuizAnswerDraftByQuestion.value = {};
    } finally {
        isLoadingStudentQuizAnswers.value = false;
    }
}

async function loadTeacherQuizAnswers() {
    if (!canManage.value || !courseId.value) {
        teacherLessonQuizAnswers.value = [];
        teacherCourseQuizAnswers.value = [];
        return;
    }

    isLoadingTeacherQuizAnswers.value = true;
    try {
        const lessonResponse = selectedLessonId.value
            ? await courseApi.getLessonQuizAnswersForTeacher(selectedLessonId.value)
            : { values: [] };
        const courseResponse = await courseApi.getCourseQuizAnswersForTeacher(courseId.value);

        teacherLessonQuizAnswers.value = normalizeAnswerRows(lessonResponse);
        teacherCourseQuizAnswers.value = normalizeAnswerRows(courseResponse);
    } catch {
        teacherLessonQuizAnswers.value = [];
        teacherCourseQuizAnswers.value = [];
    } finally {
        isLoadingTeacherQuizAnswers.value = false;
    }
}

async function loadQuizWorkspace() {
    await loadSelectedLessonQuiz();
    await loadCourseQuiz();
    await loadMyQuizAnswers();
    await loadTeacherQuizAnswers();
}

function openCreateCourseQuizEditor() {
    courseQuizEditorQuestions.value = mapQuestionsToDraft([]);
    courseQuizEditorAttemptPolicy.value = 'reattempt';
    isCourseQuizEditorOpen.value = true;
}

function openEditCourseQuizEditor() {
    courseQuizEditorQuestions.value = mapQuestionsToDraft(courseQuizQuestions.value);
    courseQuizEditorAttemptPolicy.value = getQuizAttemptPolicy(courseQuizQuestions.value);
    isCourseQuizEditorOpen.value = true;
}

function closeCourseQuizEditor() {
    isCourseQuizEditorOpen.value = false;
    courseQuizEditorQuestions.value = [];
    courseQuizEditorAttemptPolicy.value = 'reattempt';
}

async function saveCourseQuizEditor() {
    if (!canManage.value || !courseId.value || isSavingCourseQuiz.value) {
        return;
    }

    const questionsPayload = courseQuizEditorQuestions.value.map((draft) => buildQuizQuestionPayload(draft));

    if (!questionsPayload.length) {
        errorMessage.value = 'Course quiz must contain at least one question.';
        return;
    }

    if (questionsPayload.some((item) => !item.questionText || item.options.length < 2 || item.options.filter((opt) => opt.isCorrect).length !== 1)) {
        errorMessage.value = 'Each question must include text, at least two variants, and exactly one correct answer.';
        return;
    }

    clearMessages();
    isSavingCourseQuiz.value = true;
    try {
        const response = await courseApi.upsertCourseQuiz({
            courseId: courseId.value,
            questions: questionsPayload,
            attemptPolicy: normalizeAttemptPolicy(courseQuizEditorAttemptPolicy.value),
        });

        actionMessage.value = readMessage(response) || 'Course quiz saved successfully.';
        closeCourseQuizEditor();
        await loadQuizWorkspace();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not save course quiz.';
    } finally {
        isSavingCourseQuiz.value = false;
    }
}

async function submitLessonQuiz() {
    if (!selectedLessonId.value || isSubmittingLessonQuiz.value || !canEditLessonQuizAnswers()) {
        return;
    }

    if (!hasSelectedAnswersForAllQuestions(lessonQuizQuestions.value, lessonQuizAnswerDraftByQuestion.value)) {
        errorMessage.value = 'Please select answers for all lesson quiz questions before submitting.';
        return;
    }

    const answers = buildSubmitAnswersPayload(lessonQuizQuestions.value, lessonQuizAnswerDraftByQuestion.value);

    clearMessages();
    isSubmittingLessonQuiz.value = true;
    try {
        const response = await courseApi.submitLessonQuiz({
            lessonId: selectedLessonId.value,
            answers,
        });

        actionMessage.value = readMessage(response) || 'Lesson mini quiz submitted.';
        await loadMyQuizAnswers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not submit lesson mini quiz.';
    } finally {
        isSubmittingLessonQuiz.value = false;
    }
}

async function submitCourseQuiz() {
    if (!courseId.value || isSubmittingCourseQuiz.value || !canEditCourseQuizAnswers()) {
        return;
    }

    if (!hasSelectedAnswersForAllQuestions(courseQuizQuestions.value, courseQuizAnswerDraftByQuestion.value)) {
        errorMessage.value = 'Please select answers for all course quiz questions before submitting.';
        return;
    }

    const answers = buildSubmitAnswersPayload(courseQuizQuestions.value, courseQuizAnswerDraftByQuestion.value);

    clearMessages();
    isSubmittingCourseQuiz.value = true;
    try {
        const response = await courseApi.submitCourseQuiz({
            courseId: courseId.value,
            answers,
        });

        actionMessage.value = readMessage(response) || 'Course quiz submitted.';
        await loadMyQuizAnswers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not submit course quiz.';
    } finally {
        isSubmittingCourseQuiz.value = false;
    }
}

async function loadCourseProgress() {
    if (!canView.value || !courseId.value || canManage.value) {
        courseProgress.value = null;
        return;
    }

    try {
        const response = await courseApi.getCourseProgress(courseId.value);
        courseProgress.value = normalizeValue(response);
    } catch {
        courseProgress.value = null;
    }
}

async function loadStudentsProgress() {
    if (!canManage.value || !courseId.value) {
        studentsProgress.value = [];
        gradeDraftByStudent.value = {};
        return;
    }

    try {
        const response = await courseApi.getCourseStudentsProgress(courseId.value);
        const rows = normalizeStudentsProgress(response);
        studentsProgress.value = rows;

        const hasSelected = rows.some((item) => getStudentId(item) === selectedTeacherStudentId.value);
        selectedTeacherStudentId.value = hasSelected
            ? selectedTeacherStudentId.value
            : getStudentId(rows[0]);

        gradeDraftByStudent.value = rows.reduce((acc, item) => {
            const studentId = getStudentId(item);
            if (!studentId) {
                return acc;
            }

            acc[studentId] = getDraftMark(item);
            return acc;
        }, {});
    } catch (error) {
        studentsProgress.value = [];
        selectedTeacherStudentId.value = 0;
        gradeDraftByStudent.value = {};
        errorMessage.value = error?.message || 'Failed to load students progress.';
    }
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

function resetLessonForm() {
    editingLessonId.value = 0;
    editingLessonQuizQuestions.value = [];
    editingLessonQuizAttemptPolicy.value = 'reattempt';
    formErrorMessage.value = '';
    isLessonFormOpen.value = false;
}

function openCreateLessonForm() {
    editingLessonId.value = 0;
    editingLessonQuizQuestions.value = mapQuestionsToDraft([]);
    editingLessonQuizAttemptPolicy.value = 'reattempt';
    formErrorMessage.value = '';
    isLessonFormOpen.value = true;
}

async function openEditLessonForm(lesson) {
    const id = getLessonId(lesson);
    if (!id) {
        return;
    }

    editingLessonId.value = id;
    editingLessonQuizQuestions.value = [];
    formErrorMessage.value = '';

    try {
        const response = await courseApi.getLessonQuizQuestions(id);
        const questions = normalizeQuestionRows(response);
        editingLessonQuizQuestions.value = questions.length ? questions : [];
        editingLessonQuizAttemptPolicy.value = getQuizAttemptPolicy(questions);
    } catch {
        editingLessonQuizQuestions.value = [];
        editingLessonQuizAttemptPolicy.value = 'reattempt';
    }

    isLessonFormOpen.value = true;
}

function closeLessonForm() {
    resetLessonForm();
}

function getPreviewContent(item) {
    const rawContent = lessonResourcesService.removeEmbeddedResourcesFromContent(item?.content || item?.Content || '');

    if (item?.isMarkdown ?? item?.IsMarkdown) {
        return markdownService.renderMarkdownToSafeHtml(rawContent);
    }

    return rawContent;
}

async function loadUniversityMembership() {
    isLoadingWorkspace.value = true;
    clearMessages();

    try {
        authUtils.getCurrentUser();

        const [universityResponse, myUniversitiesResponse] = await Promise.all([
            universityApi.getUniversityByName(universityName.value),
            universityApi.getMyUniversities({ page: 1, pageSize: 100 }),
        ]);

        university.value = normalizeValue(universityResponse);

        const myItems = normalizeItems(myUniversitiesResponse);
        isMember.value = myItems.some((item) => String(item.name || item.Name || '').toLowerCase() === universityName.value.toLowerCase());
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load university workspace.';
        university.value = null;
        isMember.value = false;
    } finally {
        isLoadingWorkspace.value = false;
    }
}

async function loadCourse() {
    if (!courseId.value || !canView.value) {
        course.value = null;
        return;
    }

    try {
        const response = await courseApi.getCourseById(courseId.value);
        course.value = normalizeValue(response);
    } catch (error) {
        course.value = null;
        errorMessage.value = error?.message || 'Failed to load course details.';
    }
}

async function loadLessons() {
    if (!courseId.value || !canView.value) {
        lessons.value = [];
        selectedLesson.value = null;
        selectedLessonId.value = 0;
        return;
    }

    isLoadingLessons.value = true;
    formErrorMessage.value = '';

    try {
        const response = await courseApi.getCourseLessons(courseId.value);
        lessons.value = normalizeItems(response);

        if (!lessons.value.length) {
            selectedLessonId.value = 0;
            selectedLesson.value = null;
            return;
        }

        const firstId = getLessonId(lessons.value[0]);
        if (!selectedLessonId.value || !lessons.value.some((item) => getLessonId(item) === selectedLessonId.value)) {
            selectedLessonId.value = firstId;
            await loadLessonDetails(firstId);
            return;
        }

        if (selectedLessonId.value) {
            await loadLessonDetails(selectedLessonId.value);
        }
    } catch (error) {
        lessons.value = [];
        selectedLesson.value = null;
        selectedLessonId.value = 0;
        errorMessage.value = error?.message || 'Failed to load lessons.';
    } finally {
        isLoadingLessons.value = false;
    }
}

async function loadLessonDetails(lessonId) {
    if (!lessonId) {
        return;
    }

    try {
        const response = await courseApi.getLessonById(lessonId);
        selectedLesson.value = normalizeValue(response);
    } catch (error) {
        selectedLesson.value = null;
        errorMessage.value = error?.message || 'Failed to load lesson details.';
    }
}

async function refreshWorkspace() {
    await loadUniversityMembership();

    if (canView.value) {
        await loadCourse();
        await loadLessons();
        await loadCourseProgress();
        await loadStudentsProgress();
        await loadQuizWorkspace();
    }
}

async function joinUniversity() {
    if (!university.value?.isOpened || isMember.value) {
        return;
    }

    isJoining.value = true;
    clearMessages();

    try {
        const response = await universityApi.enterUniversity(universityName.value);
        actionMessage.value = readMessage(response) || 'You entered the university as student.';
        isMember.value = true;
        await loadCourse();
        await loadLessons();
        await loadCourseProgress();
        await loadQuizWorkspace();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

async function selectLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId || lessonId === selectedLessonId.value) {
        return;
    }

    selectedLessonId.value = lessonId;
    selectedLesson.value = null;
    await loadLessonDetails(lessonId);
    await loadQuizWorkspace();
}

async function completeSelectedLesson() {
    if (!selectedLessonId.value || !canCompleteSelectedLesson.value) {
        return;
    }

    clearMessages();
    isCompletingLesson.value = true;

    try {
        const response = await courseApi.completeLesson(selectedLessonId.value);
        actionMessage.value = readMessage(response) || 'Lesson marked as completed.';

        await loadLessons();
        await loadCourseProgress();
        await loadQuizWorkspace();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not complete lesson.';
    } finally {
        isCompletingLesson.value = false;
    }
}

async function saveStudentGrade(student = null) {
    if (!canManage.value || isSavingGrade.value) {
        return;
    }

    const targetStudent = student || selectedTeacherStudent.value;
    if (!targetStudent) {
        errorMessage.value = 'Select a student first.';
        return;
    }

    if (!getStudentCanBeMarked(targetStudent)) {
        errorMessage.value = 'Mark can be assigned only after the student completes all lessons and quizzes.';
        return;
    }

    const studentId = getStudentId(targetStudent);
    const mark = Number(gradeDraftByStudent.value[studentId]);

    if (!studentId || !Number.isInteger(mark) || mark < 1 || mark > 10) {
        errorMessage.value = 'Select a valid mark between 1 and 10.';
        return;
    }

    clearMessages();
    isSavingGrade.value = true;

    try {
        const response = await courseApi.assignCourseGrade({
            courseId: courseId.value,
            studentId,
            mark,
        });

        actionMessage.value = readMessage(response) || 'Student mark saved successfully.';
        await loadStudentsProgress();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not save student mark.';
    } finally {
        isSavingGrade.value = false;
    }
}

async function upsertLessonQuizForLesson(lessonId, quizPayload, attemptPolicy) {
    if (!lessonId) {
        return;
    }

    await courseApi.upsertLessonQuiz({
        lessonId,
        questions: Array.isArray(quizPayload) ? quizPayload : [],
        attemptPolicy: normalizeAttemptPolicy(attemptPolicy),
    });
}

async function submitLessonForm(payload) {
    formErrorMessage.value = payload.validationError || '';
    if (payload.validationError) {
        return;
    }

    clearMessages();
    isSubmittingLesson.value = true;

    try {
        const resourceFields = lessonResourcesService.buildApiResourceFields(payload.resources);
        const lessonQuizPayload = payload.lessonQuizQuestions;
        const lessonQuizAttemptPolicy = normalizeAttemptPolicy(payload.lessonQuizAttemptPolicy);

        if (isEditingMode.value) {
            const response = await courseApi.updateLesson({
                lessonId: editingLessonId.value,
                title: payload.title,
                content: payload.content,
                videoLink: resourceFields.videoLink,
                materialLink: resourceFields.materialLink,
                resources: resourceFields.resources,
                orderNumber: payload.orderNumber,
            });

            await upsertLessonQuizForLesson(editingLessonId.value, lessonQuizPayload, lessonQuizAttemptPolicy);

            actionMessage.value = readMessage(response) || 'Lesson updated successfully.';
            await loadLessons();
            await loadCourseProgress();
            await loadStudentsProgress();
            await loadQuizWorkspace();
            if (selectedLessonId.value) {
                await loadLessonDetails(selectedLessonId.value);
            }
        } else {
            const response = await courseApi.createLesson({
                courseId: courseId.value,
                title: payload.title,
                content: payload.content,
                isMarkdown: payload.isMarkdown,
                videoLink: resourceFields.videoLink,
                materialLink: resourceFields.materialLink,
                resources: resourceFields.resources,
                orderNumber: payload.orderNumber,
            });

            actionMessage.value = readMessage(response) || 'Lesson created successfully.';
            await loadLessons();
            await loadCourseProgress();
            await loadStudentsProgress();
            await loadQuizWorkspace();

            if (lessons.value.length) {
                const newestLessonId = getLessonId(lessons.value[lessons.value.length - 1]);
                if (newestLessonId) {
                    await upsertLessonQuizForLesson(newestLessonId, lessonQuizPayload, lessonQuizAttemptPolicy);
                    selectedLessonId.value = newestLessonId;
                    await loadLessonDetails(newestLessonId);
                    await loadQuizWorkspace();
                }
            }
        }

        resetLessonForm();
        editingLessonQuizQuestions.value = [];
    } catch (error) {
        formErrorMessage.value = error?.message || 'Could not save lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

async function removeLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId || !canManage.value || isSubmittingLesson.value) {
        return;
    }

    clearMessages();
    isSubmittingLesson.value = true;

    try {
        const response = await courseApi.deleteLesson(lessonId);
        actionMessage.value = readMessage(response) || 'Lesson deleted successfully.';

        if (editingLessonId.value === lessonId) {
            resetLessonForm();
        }

        const deletedWasSelected = selectedLessonId.value === lessonId;
        await loadLessons();
        await loadCourseProgress();
        await loadStudentsProgress();
        await loadQuizWorkspace();

        if (deletedWasSelected && lessons.value.length) {
            const fallbackId = getLessonId(lessons.value[0]);
            selectedLessonId.value = fallbackId;
            await loadLessonDetails(fallbackId);
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not delete lesson.';
    } finally {
        isSubmittingLesson.value = false;
    }
}

function openCoursesWorkspace() {
    router.push({ name: 'UniversityCourses', params: { name: universityName.value } });
}

onMounted(async () => {
    authUtils.getCurrentUser();
    await refreshWorkspace();
});
</script>

<template>
    <main class="page-shell">
        <UniversityContextHeader
            :university-name="universityName"
            active-tab="courses"
            subtitle="Main study space for course work."
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card workspace-content">
            <div class="workspace-content__heading-row">
                <div>
                    <p class="section-kicker">Course lessons</p>
                    <h2 class="section-title">{{ getCourseTitle() }}</h2>
                </div>

                <div class="workspace-content__actions">
                    <button class="secondary-button" type="button" @click="openCoursesWorkspace">Back to courses</button>
                    <button class="secondary-button" type="button" @click="refreshWorkspace" :disabled="isLoadingWorkspace || isLoadingLessons">
                        {{ isLoadingWorkspace || isLoadingLessons ? 'Refreshing...' : 'Refresh workspace' }}
                    </button>
                    <button
                        v-if="canManage"
                        class="submit-button"
                        type="button"
                        @click="openCreateLessonForm"
                        :disabled="isSubmittingLesson || isLoadingLessons"
                    >
                        Create lesson
                    </button>
                </div>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article class="hero-placeholder surface-card">
                <AppIcon name="courses" />
                <div>
                    <h3>{{ canView ? 'Lesson workspace' : 'Join to access lessons' }}</h3>
                    <p class="section-copy">
                        {{
                            canView
                                ? 'Read lesson content, open supplementary links, and keep learning flow clear through ordered lessons.'
                                : 'Membership is required to read lessons. Use join to enter this university as a student.'
                        }}
                    </p>
                </div>
            </article>

            <article v-if="canView && hasLessons" class="progress-card surface-card">
                <div class="progress-card__head">
                    <p class="section-kicker">Course progression</p>
                    <p class="progress-card__status">{{ progressCompletedCount }} / {{ progressTotalCount }} completed</p>
                </div>
                <div class="progress-card__track" role="progressbar" :aria-valuenow="progressPercent" aria-valuemin="0" aria-valuemax="100">
                    <div class="progress-card__fill" :style="{ width: `${progressPercent}%` }" />
                </div>
                <div class="progress-card__row">
                    <p class="section-copy progress-card__percent">{{ progressPercent }}% complete</p>
                    <span v-if="studentMark" class="pill pill--pink">Current mark {{ studentMark }}/10</span>
                </div>
            </article>

            <LessonFormPanel
                v-if="isLessonFormOpen"
                :mode="isEditingMode ? 'edit' : 'create'"
                :initial-lesson="editingLesson"
                :initial-resources="editingLessonResources"
                :initial-quiz-questions="editingLessonQuizQuestions"
                :initial-quiz-attempt-policy="editingLessonQuizAttemptPolicy"
                :is-submitting="isSubmittingLesson"
                :error-message="formErrorMessage"
                @submit="submitLessonForm"
                @cancel="closeLessonForm"
            />

            <div v-if="canView" class="lesson-layout">
                <aside class="lesson-list surface-card">
                    <p class="section-kicker">Lessons in course</p>

                    <transition-group name="fade-rise" tag="div" class="lesson-list__items">
                        <LessonCard
                            v-for="item in lessons"
                            :key="item.id || item.Id"
                            :lesson="item"
                            :is-active="selectedLessonId === getLessonId(item)"
                            :can-manage="canManage"
                            :disable-actions="isSubmittingLesson || isLoadingLessons"
                            @open="selectLesson"
                            @edit="openEditLessonForm"
                            @delete="removeLesson"
                        />
                    </transition-group>

                    <p v-if="!isLoadingLessons && !hasLessons" class="state-message">
                        No lessons yet. {{ canManage ? 'Create the first lesson to start the course.' : 'A teacher will publish lessons soon.' }}
                    </p>
                </aside>

                <article class="lesson-view surface-card" v-if="selectedLessonResolved">
                    <div class="lesson-view__heading">
                        <div>
                            <p class="section-kicker">Selected lesson</p>
                            <h3>{{ selectedLessonResolved.title || selectedLessonResolved.Title }}</h3>
                        </div>
                        <span class="pill pill--accent">Order {{ selectedLessonResolved.orderNumber || selectedLessonResolved.OrderNumber }}</span>
                    </div>

                    <div
                        v-if="isSelectedLessonMarkdown"
                        class="lesson-view__content lesson-view__content--markdown section-copy"
                        v-html="getPreviewContent(selectedLessonResolved)"
                    />
                    <p v-else class="lesson-view__content lesson-view__content--plain section-copy">{{ getPreviewContent(selectedLessonResolved) }}</p>

                    <div v-if="!canManage" class="lesson-view__completion">
                        <p class="section-copy">
                            {{ selectedLessonCompleted ? 'You have completed this lesson.' : 'Mark this lesson as complete when you finish studying it.' }}
                        </p>
                        <button
                            class="submit-button"
                            type="button"
                            :disabled="!canCompleteSelectedLesson"
                            @click="completeSelectedLesson"
                        >
                            {{ isCompletingLesson ? 'Saving...' : selectedLessonCompleted ? 'Completed' : 'Mark as complete' }}
                        </button>
                    </div>

                    <section v-if="canManage || hasLessonQuizQuestions" class="quiz-panel surface-card">
                        <div class="quiz-panel__head">
                            <div>
                                <p class="section-kicker">Lesson mini quiz</p>
                                <h4>Check understanding at lesson end</h4>
                            </div>
                            <p class="section-copy">{{ hasLessonQuizQuestions ? `${lessonQuizQuestions.length} question(s)` : 'No lesson mini quiz questions yet.' }}</p>
                        </div>

                        <p v-if="canManage" class="state-message">Quiz for this lesson is configured only inside lesson create/edit form.</p>
                        <p v-if="hasLessonQuizQuestions" class="quiz-question-card__policy">Policy: {{ isSingleAttemptLessonQuiz() ? 'Single attempt for whole quiz' : 'Reattempt allowed for whole quiz' }}</p>

                        <div v-if="hasLessonQuizQuestions" class="quiz-questions-list">
                            <article v-for="question in lessonQuizQuestions" :key="getQuestionId(question)" class="quiz-question-card">
                                <p class="quiz-question-card__question">{{ question.questionText || question.QuestionText }}</p>

                                <div class="quiz-question-card__options">
                                    <label v-for="option in question.options || question.Options || []" :key="getOptionId(option)" class="quiz-question-card__option">
                                        <input
                                            v-model.number="lessonQuizAnswerDraftByQuestion[getQuestionId(question)]"
                                            :disabled="!canEditLessonQuizAnswers() || isSubmittingLessonQuiz"
                                            type="radio"
                                            :name="`lesson-question-${getQuestionId(question)}`"
                                            :value="getOptionId(option)"
                                        />
                                        <span>{{ option.optionText || option.OptionText }}</span>
                                        <span v-if="canManage && (option.isCorrect ?? option.IsCorrect)" class="pill pill--accent">Correct</span>
                                    </label>
                                </div>
                            </article>
                        </div>

                        <div v-if="!canManage && hasLessonQuizQuestions" class="quiz-panel__actions">
                            <button
                                class="submit-button"
                                type="button"
                                :disabled="isSubmittingLessonQuiz || !hasSelectedAnswersForAllQuestions(lessonQuizQuestions, lessonQuizAnswerDraftByQuestion) || !canEditLessonQuizAnswers()"
                                @click="submitLessonQuiz"
                            >
                                {{ isSubmittingLessonQuiz ? 'Submitting...' : 'Submit whole quiz' }}
                            </button>
                        </div>
                        <p v-if="!canManage && isSingleAttemptLessonQuiz() && myLessonQuizAnswers.length" class="quiz-question-card__lock">
                            This whole lesson quiz is locked after your first submit.
                        </p>

                        <p v-else-if="!isLoadingLessonQuiz" class="state-message">No lesson mini quiz questions published yet.</p>

                        <div v-if="!canManage && myLessonQuizAnswers.length" class="quiz-score-card">
                            <p class="section-kicker">Your lesson mini quiz score</p>
                            <p class="quiz-score-card__value">{{ myLessonQuizScoreLabel }}</p>
                        </div>
                    </section>

                    <div class="lesson-view__links" v-if="selectedLessonResources.length">
                        <a
                            v-for="resource in selectedLessonResources"
                            :key="resource.url"
                            class="secondary-button lesson-view__link"
                            :href="resource.url"
                            target="_blank"
                            rel="noreferrer noopener"
                        >
                            {{ resource.title }}
                        </a>
                    </div>
                </article>

                <article class="lesson-view surface-card" v-else>
                    <p class="section-copy">Select a lesson to view full content.</p>
                </article>
            </div>

            <section v-if="canView" class="quiz-panel surface-card">
                <div class="quiz-panel__head">
                    <div>
                        <p class="section-kicker">Course quiz mode</p>
                        <h4>Optional quizzes instead of lesson content</h4>
                    </div>
                    <p class="section-copy">{{ hasCourseQuizQuestions ? `${courseQuizQuestions.length} question(s)` : 'No course-level quiz questions yet.' }}</p>
                </div>

                <div v-if="canManage" class="quiz-panel__actions">
                    <button class="submit-button" type="button" :disabled="isSavingCourseQuiz" @click="hasCourseQuizQuestions ? openEditCourseQuizEditor() : openCreateCourseQuizEditor()">
                        {{ hasCourseQuizQuestions ? 'Edit course quiz' : 'Create course quiz' }}
                    </button>
                </div>

                <p v-if="hasCourseQuizQuestions" class="quiz-question-card__policy">Policy: {{ isSingleAttemptCourseQuiz() ? 'Single attempt for whole quiz' : 'Reattempt allowed for whole quiz' }}</p>

                <div v-if="canManage && isCourseQuizEditorOpen" class="quiz-creator">
                    <div class="field-group">
                        <label class="field-label">Attempt policy for whole quiz</label>
                        <select v-model="courseQuizEditorAttemptPolicy" class="input-field">
                            <option value="reattempt">Reattempt allowed</option>
                            <option value="single">Single attempt (lock after first quiz submit)</option>
                        </select>
                    </div>

                    <div class="quiz-creator__questions">
                        <article v-for="(questionDraft, questionIndex) in courseQuizEditorQuestions" :key="`course-question-draft-${questionIndex}`" class="quiz-creator__question-card">
                            <div class="field-group">
                                <label class="field-label">Question {{ questionIndex + 1 }}</label>
                                <input v-model="questionDraft.questionText" class="input-field" type="text" maxlength="400" placeholder="What does HTTP status 404 mean?" />
                            </div>

                            <div class="quiz-creator__options">
                                <div v-for="(optionText, optionIndex) in questionDraft.optionTexts" :key="`course-opt-${questionIndex}-${optionIndex}`" class="quiz-creator__option-row">
                                    <input v-model="questionDraft.optionTexts[optionIndex]" class="input-field" type="text" maxlength="300" :placeholder="`Variant ${optionIndex + 1}`" />
                                    <label class="quiz-creator__correct">
                                        <input v-model.number="questionDraft.correctOptionIndex" type="radio" :name="`course-quiz-correct-${questionIndex}`" :value="optionIndex" />
                                        Correct
                                    </label>
                                    <button class="secondary-button" type="button" :disabled="questionDraft.optionTexts.length <= 2" @click="removeOptionFromQuestion(courseQuizEditorQuestions, questionIndex, optionIndex)">
                                        Remove
                                    </button>
                                </div>
                            </div>

                            <div class="quiz-panel__actions">
                                <button class="secondary-button" type="button" @click="addOptionToQuestion(courseQuizEditorQuestions, questionIndex)">Add variant</button>
                                <button class="secondary-button" type="button" :disabled="courseQuizEditorQuestions.length <= 1" @click="removeQuestionDraftRow(courseQuizEditorQuestions, questionIndex)">
                                    Remove question
                                </button>
                            </div>
                        </article>
                    </div>

                    <div class="quiz-panel__actions">
                        <button class="secondary-button" type="button" @click="addQuestionDraftRow(courseQuizEditorQuestions)">Add question</button>
                        <button class="submit-button" type="button" :disabled="isSavingCourseQuiz" @click="saveCourseQuizEditor">
                            {{ isSavingCourseQuiz ? 'Saving...' : 'Save whole quiz' }}
                        </button>
                        <button class="secondary-button" type="button" :disabled="isSavingCourseQuiz" @click="closeCourseQuizEditor">Cancel</button>
                    </div>
                </div>

                <div v-if="hasCourseQuizQuestions" class="quiz-questions-list">
                    <article v-for="question in courseQuizQuestions" :key="getQuestionId(question)" class="quiz-question-card">
                        <p class="quiz-question-card__question">{{ question.questionText || question.QuestionText }}</p>

                        <div class="quiz-question-card__options">
                            <label v-for="option in question.options || question.Options || []" :key="getOptionId(option)" class="quiz-question-card__option">
                                <input
                                    v-model.number="courseQuizAnswerDraftByQuestion[getQuestionId(question)]"
                                    :disabled="!canEditCourseQuizAnswers() || isSubmittingCourseQuiz"
                                    type="radio"
                                    :name="`course-question-${getQuestionId(question)}`"
                                    :value="getOptionId(option)"
                                />
                                <span>{{ option.optionText || option.OptionText }}</span>
                                <span v-if="canManage && (option.isCorrect ?? option.IsCorrect)" class="pill pill--accent">Correct</span>
                            </label>
                        </div>
                    </article>
                </div>

                <div v-if="!canManage && hasCourseQuizQuestions" class="quiz-panel__actions">
                    <button
                        class="submit-button"
                        type="button"
                        :disabled="isSubmittingCourseQuiz || !hasSelectedAnswersForAllQuestions(courseQuizQuestions, courseQuizAnswerDraftByQuestion) || !canEditCourseQuizAnswers()"
                        @click="submitCourseQuiz"
                    >
                        {{ isSubmittingCourseQuiz ? 'Submitting...' : 'Submit whole quiz' }}
                    </button>
                </div>
                <p v-if="!canManage && isSingleAttemptCourseQuiz() && myCourseQuizAnswers.length" class="quiz-question-card__lock">
                    This whole course quiz is locked after your first submit.
                </p>

                <p v-else-if="!isLoadingCourseQuiz" class="state-message">No course-level quiz questions published yet.</p>

                <div v-if="!canManage && myCourseQuizAnswers.length" class="quiz-score-card">
                    <p class="section-kicker">Your course quiz score</p>
                    <p class="quiz-score-card__value">{{ myCourseQuizScoreLabel }}</p>
                </div>
            </section>

            <article v-if="canManage" class="surface-card grading-panel">
                <div class="grading-panel__head">
                    <div>
                        <p class="section-kicker">Teacher panel</p>
                        <h3>Course grading</h3>
                    </div>
                    <p class="section-copy">Assign marks from 1 to 10 only for students with completed courses.</p>
                </div>

                <div v-if="studentsProgress.length" class="teacher-panel">
                    <aside class="teacher-student-list">
                        <p class="section-kicker">Students</p>
                        <button
                            v-for="student in studentsProgress"
                            :key="getStudentId(student)"
                            class="teacher-student-list__item"
                            :class="{ 'teacher-student-list__item--active': selectedTeacherStudentId === getStudentId(student) }"
                            type="button"
                            @click="selectedTeacherStudentId = getStudentId(student)"
                        >
                            <p class="teacher-student-list__name">{{ student.studentName || student.StudentName || 'Unknown' }}</p>
                            <p class="teacher-student-list__meta">
                                Lessons: {{ student.completedLessons || student.CompletedLessons || 0 }} / {{ student.totalLessons || student.TotalLessons || 0 }}
                            </p>
                            <p class="teacher-student-list__meta">
                                Quizzes: {{ student.completedQuizQuestions || student.CompletedQuizQuestions || 0 }} / {{ student.totalQuizQuestions || student.TotalQuizQuestions || 0 }}
                            </p>
                        </button>
                    </aside>

                    <section v-if="selectedTeacherStudent" class="teacher-student-details">
                        <div class="teacher-student-details__head">
                            <h4>{{ selectedTeacherStudent.studentName || selectedTeacherStudent.StudentName || 'Unknown' }}</h4>
                            <div class="teacher-student-details__badges">
                                <span class="pill" :class="getStudentLessonsCompleted(selectedTeacherStudent) ? 'pill--accent' : 'pill--pink'">
                                    {{ getStudentLessonsCompleted(selectedTeacherStudent) ? 'Lessons completed' : 'Lessons incomplete' }}
                                </span>
                                <span class="pill" :class="getStudentQuizzesCompleted(selectedTeacherStudent) ? 'pill--accent' : 'pill--pink'">
                                    {{ getStudentQuizzesCompleted(selectedTeacherStudent) ? 'Quizzes completed' : 'Quizzes incomplete' }}
                                </span>
                            </div>
                        </div>

                        <div class="teacher-student-details__marking surface-card">
                            <p class="section-kicker">Marking</p>
                            <p class="section-copy">Marking is available only after all lessons and all quizzes are completed.</p>
                            <div class="teacher-student-details__marking-row">
                                <select
                                    class="input-field grading-table__mark"
                                    :value="gradeDraftByStudent[getStudentId(selectedTeacherStudent)]"
                                    :disabled="isSavingGrade || !selectedTeacherStudentCanBeMarked"
                                    @change="gradeDraftByStudent[getStudentId(selectedTeacherStudent)] = Number($event.target.value)"
                                >
                                    <option v-for="value in 10" :key="value" :value="value">{{ value }}</option>
                                </select>
                                <button
                                    class="secondary-button"
                                    type="button"
                                    :disabled="isSavingGrade || !selectedTeacherStudentCanBeMarked"
                                    @click="saveStudentGrade(selectedTeacherStudent)"
                                >
                                    {{ isSavingGrade ? 'Saving...' : 'Save mark' }}
                                </button>
                            </div>
                            <p v-if="!selectedTeacherStudentCanBeMarked" class="state-message">Student must complete all lessons and quizzes before marking.</p>
                        </div>

                        <div class="quiz-review">
                            <p class="section-kicker">Quiz answers review</p>

                            <div class="quiz-review__tables">
                                <div class="quiz-review__table">
                                    <p class="quiz-review__title">Selected lesson mini quiz answers</p>
                                    <div v-if="selectedTeacherStudentLessonQuizAnswers.length" class="quiz-review__rows">
                                        <div v-for="(answer, index) in selectedTeacherStudentLessonQuizAnswers" :key="`lesson-answer-${index}`" class="quiz-review__row">
                                            <p>{{ answer.questionText || answer.QuestionText }}</p>
                                            <p>Selected: {{ answer.selectedOptionText || answer.SelectedOptionText }}</p>
                                            <p>Correct: {{ answer.correctOptionText || answer.CorrectOptionText }}</p>
                                            <span class="pill" :class="(answer.isCorrect || answer.IsCorrect) ? 'pill--accent' : 'pill--pink'">
                                                {{ (answer.isCorrect || answer.IsCorrect) ? 'Correct' : 'Incorrect' }}
                                            </span>
                                        </div>
                                    </div>
                                    <p v-else class="state-message">No lesson mini quiz answers for this student yet.</p>
                                </div>

                                <div class="quiz-review__table">
                                    <p class="quiz-review__title">Course quiz answers</p>
                                    <div v-if="selectedTeacherStudentCourseQuizAnswers.length" class="quiz-review__rows">
                                        <div v-for="(answer, index) in selectedTeacherStudentCourseQuizAnswers" :key="`course-answer-${index}`" class="quiz-review__row">
                                            <p>{{ answer.questionText || answer.QuestionText }}</p>
                                            <p>Selected: {{ answer.selectedOptionText || answer.SelectedOptionText }}</p>
                                            <p>Correct: {{ answer.correctOptionText || answer.CorrectOptionText }}</p>
                                            <span class="pill" :class="(answer.isCorrect || answer.IsCorrect) ? 'pill--accent' : 'pill--pink'">
                                                {{ (answer.isCorrect || answer.IsCorrect) ? 'Correct' : 'Incorrect' }}
                                            </span>
                                        </div>
                                    </div>
                                    <p v-else class="state-message">No course quiz answers for this student yet.</p>
                                </div>
                            </div>
                        </div>
                    </section>
                </div>

                <p v-else class="state-message">Student activity will appear here after lesson completion starts.</p>
            </article>

            <p v-if="!canView" class="state-message">Membership is required to view lessons in this course.</p>
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

.workspace-content {
    padding: 1.2rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.workspace-content__heading-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.8rem;
}

.workspace-content__actions {
    display: flex;
    gap: 0.65rem;
    align-items: center;
    flex-wrap: wrap;
    justify-content: flex-end;
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

.hero-placeholder {
    padding: 1rem;
    display: flex;
    align-items: flex-start;
    gap: 0.8rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.06));
}

.hero-placeholder h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.hero-placeholder p {
    margin: 0.3rem 0 0;
    color: var(--ttl-text-secondary);
    line-height: 1.6;
}

.progress-card {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.progress-card__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.7rem;
}

.progress-card__status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.86rem;
}

.progress-card__track {
    height: 0.62rem;
    border-radius: 0.6rem;
    background: rgba(143, 44, 226, 0.12);
    overflow: hidden;
}

.progress-card__fill {
    height: 100%;
    border-radius: 0.6rem;
    background: linear-gradient(90deg, rgba(143, 44, 226, 0.85), rgba(255, 95, 162, 0.85));
    transition: width var(--ttl-transition-base);
}

.progress-card__percent {
    margin: 0;
}

.progress-card__row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.lesson-layout {
    display: grid;
    grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.25fr);
    gap: 0.9rem;
}

.lesson-list,
.lesson-view {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}

.lesson-list__items {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.lesson-view__heading {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.7rem;
}

.lesson-view__heading h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.lesson-view__content {
    margin: 0;
    line-height: 1.7;
}

.lesson-view__content--plain {
    white-space: pre-wrap;
}

.lesson-view__content--markdown {
    white-space: normal;
}

.lesson-view__links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.55rem;
}

.lesson-view__completion {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.8rem;
    padding: 0.7rem;
    border-radius: var(--ttl-radius-md);
    background: var(--ttl-bg-surface-soft);
    border: 1px solid var(--ttl-border-subtle);
}

.lesson-view__completion .section-copy {
    margin: 0;
}

.lesson-view__link {
    text-decoration: none;
    min-height: 2.25rem;
}

.quiz-panel {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.quiz-panel__head {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.7rem;
}

.quiz-panel__head h4 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.quiz-creator {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    padding: 0.75rem;
    border-radius: var(--ttl-radius-md);
    border: 1px solid var(--ttl-border-subtle);
    background: var(--ttl-bg-surface-soft);
}

.quiz-creator__options {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.quiz-creator__questions {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.quiz-creator__question-card {
    padding: 0.65rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.quiz-creator__option-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    align-items: center;
    gap: 0.55rem;
}

.quiz-panel__actions {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    flex-wrap: wrap;
}

.quiz-creator__correct {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    color: var(--ttl-text-secondary);
    font-size: 0.85rem;
}

.quiz-questions-list {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.quiz-question-card {
    padding: 0.7rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.quiz-question-card__question {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.quiz-question-card__policy {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
}

.quiz-question-card__options {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.quiz-question-card__option {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    color: var(--ttl-text-secondary);
    font-size: 0.9rem;
}

.quiz-question-card__lock {
    margin: 0;
    color: var(--ttl-warning);
    font-size: 0.82rem;
}

.quiz-score-card {
    padding: 0.65rem;
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-muted);
    border: 1px solid var(--ttl-border-subtle);
}

.quiz-score-card__value {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 1.05rem;
    font-weight: 800;
}

.grading-panel {
    padding: 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.grading-panel__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.8rem;
}

.grading-panel__head h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.grading-table {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.grading-table__row {
    display: grid;
    grid-template-columns: minmax(0, 1.2fr) minmax(0, 1.1fr) minmax(0, 0.6fr) auto;
    gap: 0.6rem;
    align-items: center;
    padding: 0.6rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
}

.grading-table__row p {
    margin: 0;
}

.grading-table__row--head {
    background: var(--ttl-bg-muted);
    color: var(--ttl-text-secondary);
    font-size: 0.85rem;
    font-weight: 700;
}

.grading-table__student {
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.grading-table__progress {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    color: var(--ttl-text-secondary);
}

.grading-table__done {
    padding-top: 0.25rem;
    padding-bottom: 0.25rem;
}

.grading-table__mark {
    min-height: 2.2rem;
    width: 100%;
}

.teacher-panel {
    display: grid;
    grid-template-columns: minmax(220px, 0.8fr) minmax(0, 1.4fr);
    gap: 0.7rem;
}

.teacher-student-list {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
    padding: 0.55rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
}

.teacher-student-list__item {
    text-align: left;
    padding: 0.55rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-surface-soft);
    cursor: pointer;
}

.teacher-student-list__item--active {
    border-color: rgba(143, 44, 226, 0.35);
    background: rgba(143, 44, 226, 0.08);
}

.teacher-student-list__name {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.teacher-student-list__meta {
    margin: 0.2rem 0 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
}

.teacher-student-details {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.teacher-student-details__head h4 {
    margin: 0;
    color: var(--ttl-text-primary);
}

.teacher-student-details__badges {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
    margin-top: 0.4rem;
}

.teacher-student-details__marking {
    padding: 0.65rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.teacher-student-details__marking-row {
    display: grid;
    grid-template-columns: minmax(0, 130px) auto;
    gap: 0.55rem;
    align-items: center;
}

.quiz-review {
    margin-top: 0.35rem;
    padding-top: 0.35rem;
    border-top: 1px solid var(--ttl-border-subtle);
}

.quiz-review__tables {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.65rem;
}

.quiz-review__table {
    padding: 0.55rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.quiz-review__title {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 0.92rem;
    font-weight: 800;
}

.quiz-review__rows {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.quiz-review__row {
    padding: 0.45rem;
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-surface-soft);
    border: 1px solid var(--ttl-border-subtle);
}

.quiz-review__row p {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
}

@media (max-width: 960px) {
    .lesson-layout {
        grid-template-columns: 1fr;
    }

    .grading-table__row {
        grid-template-columns: 1fr;
    }

    .teacher-panel {
        grid-template-columns: 1fr;
    }

    .quiz-review__tables {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 700px) {
    .workspace-content__heading-row {
        flex-direction: column;
        align-items: flex-start;
    }

    .workspace-content__actions {
        width: 100%;
    }

    .workspace-content__actions .secondary-button,
    .workspace-content__actions .submit-button {
        width: 100%;
    }

    .progress-card__row,
    .lesson-view__completion,
    .grading-panel__head,
    .quiz-panel__head {
        flex-direction: column;
        align-items: flex-start;
    }

    .lesson-view__completion .submit-button {
        width: 100%;
    }

    .quiz-creator__option-row {
        grid-template-columns: 1fr;
    }

    .teacher-student-details__marking-row {
        grid-template-columns: 1fr;
    }
}
</style>
