<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UniversityContextHeader from '../components/UniversityContextHeader.vue';
import universityApi from '../services/universityApi';
import authUtils, { canManageUniversityContent, user } from '@/Shared/services/utils';
import courseApi from '@/Courses/services/courseApi';
import LessonListMobileItem from '@/Courses/components/LessonListMobileItem.vue';
import AppIcon from '@/Shared/components/AppIcon.vue';

const route = useRoute();
const router = useRouter();

const university = ref(null);
const course = ref(null);
const lessons = ref([]);
const courseProgress = ref(null);

const studentsProgress = ref([]);
const selectedTeacherStudentId = ref(0);
const teacherStudentSearchQuery = ref('');
const teacherViewMode = ref('list');
const selectedLessonReviewId = ref(0);
const teacherLessonQuizAnswers = ref([]);
const teacherCourseQuizAnswers = ref([]);
const gradeDraftByStudent = ref({});
const courseQuizQuestions = ref([]);
const myCourseQuizAnswers = ref([]);
const courseQuizAnswerDraftByQuestion = ref({});

const isLoadingWorkspace = ref(false);
const isLoadingLessons = ref(false);
const isJoining = ref(false);
const isSavingGrade = ref(false);
const isLoadingTeacherInsights = ref(false);
const isLoadingLessonAnswers = ref(false);
const isLoadingCourseQuiz = ref(false);
const isLoadingMyCourseQuizAnswers = ref(false);
const isSubmittingCourseQuiz = ref(false);
const isSavingCourseQuiz = ref(false);
const isCourseQuizEditorOpen = ref(false);
const courseQuizEditorQuestions = ref([]);
const courseQuizEditorAttemptPolicy = ref('reattempt');

const isMember = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const universityName = computed(() => String(route.params.name || ''));
const courseId = computed(() => Number(route.params.courseId || 0));

const canManage = computed(() => isMember.value && canManageUniversityContent(user.value.role));
const canView = computed(() => isMember.value);
const hasLessons = computed(() => lessons.value.length > 0);

const joinLabel = computed(() => canManageUniversityContent(user.value.role) ? 'Enter as student' : 'Enter university');
const joinVisible = computed(() => !isMember.value && !!university.value?.isOpened);

const progressCompletedCount = computed(() => Number(courseProgress.value?.completedLessons || courseProgress.value?.CompletedLessons || 0));
const progressTotalCount = computed(() => Number(courseProgress.value?.totalLessons || courseProgress.value?.TotalLessons || lessons.value.length));
const progressPercent = computed(() => {
    if (!progressTotalCount.value) {
        return 0;
    }

    const rawPercent = Number(courseProgress.value?.percentComplete || courseProgress.value?.PercentComplete || 0);
    if (Number.isFinite(rawPercent)) {
        return Math.max(0, Math.min(100, rawPercent));
    }

    return Math.round((progressCompletedCount.value / progressTotalCount.value) * 100);
});

const selectedTeacherStudent = computed(() => {
    if (!studentsProgress.value.length) {
        return null;
    }

    const selected = studentsProgress.value.find((item) => getStudentId(item) === selectedTeacherStudentId.value);
    return selected || studentsProgress.value[0] || null;
});

const filteredStudentsProgress = computed(() => {
    const rows = [...studentsProgress.value];
    const query = normalizeSearchValue(teacherStudentSearchQuery.value);

    if (!query) {
        return rows;
    }

    const maxDistance = Math.max(2, Math.floor(query.length * 0.45));

    return rows
        .map((student) => {
            const studentName = getStudentName(student);
            const score = computeSearchScore(studentName, query);
            return { student, score };
        })
        .filter((item) => item.score <= maxDistance)
        .sort((a, b) => a.score - b.score || getStudentName(a.student).localeCompare(getStudentName(b.student)))
        .map((item) => item.student);
});

const selectedTeacherStudentCanBeMarked = computed(() => {
    const student = selectedTeacherStudent.value;
    if (!student) {
        return false;
    }

    return getStudentCanBeMarked(student);
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

const hasCourseQuizQuestions = computed(() => courseQuizQuestions.value.length > 0);
const myCourseQuizCorrectCount = computed(() => myCourseQuizAnswers.value.filter(item => item.isCorrect || item.IsCorrect).length);
const myCourseQuizScoreLabel = computed(() => `${myCourseQuizCorrectCount.value}/${myCourseQuizAnswers.value.length}`);
const isFinalQuizUnlocked = computed(() => {
    if (!canView.value || canManage.value) {
        return false;
    }

    if (!progressTotalCount.value) {
        return false;
    }

    return progressCompletedCount.value >= progressTotalCount.value;
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

function getCourseTitle() {
    return course.value?.title || course.value?.Title || 'Course lessons';
}

function getLessonId(lesson) {
    return Number(lesson?.id || lesson?.Id || 0);
}

function getOrderNumber(lesson) {
    return Number(lesson?.orderNumber || lesson?.OrderNumber || 0);
}

function getStudentId(item) {
    return Number(item?.studentId || item?.StudentId || 0);
}

function getStudentName(item) {
    return String(item?.studentName || item?.StudentName || 'Unknown');
}

function normalizeSearchValue(raw) {
    return String(raw || '').trim().toLowerCase();
}

function levenshteinDistance(source, target) {
    const left = normalizeSearchValue(source);
    const right = normalizeSearchValue(target);

    if (!left.length) {
        return right.length;
    }

    if (!right.length) {
        return left.length;
    }

    const matrix = Array.from({ length: left.length + 1 }, () => Array(right.length + 1).fill(0));

    for (let row = 0; row <= left.length; row += 1) {
        matrix[row][0] = row;
    }

    for (let column = 0; column <= right.length; column += 1) {
        matrix[0][column] = column;
    }

    for (let row = 1; row <= left.length; row += 1) {
        for (let column = 1; column <= right.length; column += 1) {
            const substitutionCost = left[row - 1] === right[column - 1] ? 0 : 1;

            matrix[row][column] = Math.min(
                matrix[row - 1][column] + 1,
                matrix[row][column - 1] + 1,
                matrix[row - 1][column - 1] + substitutionCost,
            );
        }
    }

    return matrix[left.length][right.length];
}

function computeSearchScore(studentName, query) {
    const normalizedName = normalizeSearchValue(studentName);
    if (!query || !normalizedName) {
        return Number.MAX_SAFE_INTEGER;
    }

    if (normalizedName.includes(query)) {
        return 0;
    }

    const tokens = normalizedName.split(/\s+/).filter(Boolean);
    const tokenDistances = tokens.map((token) => levenshteinDistance(token, query));
    const fullDistance = levenshteinDistance(normalizedName, query);

    return Math.min(fullDistance, ...tokenDistances);
}

function getStudentCanBeMarked(item) {
    const explicit = item?.canBeMarked ?? item?.CanBeMarked;
    if (explicit !== undefined && explicit !== null) {
        return Boolean(explicit);
    }

    const lessonsCompleted = Boolean(item?.isCompleted ?? item?.IsCompleted);
    const quizzesCompleted = Boolean(item?.areAllQuizzesCompleted ?? item?.AreAllQuizzesCompleted);
    return lessonsCompleted && quizzesCompleted;
}

function normalizeAnswerRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function normalizeQuestionRows(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function getQuestionId(item) {
    return Number(item?.id || item?.Id || 0);
}

function getOptionId(item) {
    return Number(item?.id || item?.Id || 0);
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

function isSingleAttemptCourseQuiz() {
    return getQuizAttemptPolicy(courseQuizQuestions.value) === 'single';
}

function canEditCourseQuizAnswers() {
    if (canManage.value) {
        return false;
    }

    return !(isSingleAttemptCourseQuiz() && myCourseQuizAnswers.value.length > 0);
}

function getExistingAnswerOptionId(questionIdValue, answers) {
    const row = answers.find(item => Number(item?.questionId || item?.QuestionId) === questionIdValue);
    if (!row) {
        return 0;
    }

    return Number(row?.selectedOptionId || row?.SelectedOptionId || 0);
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
    return (Array.isArray(questions) ? questions : []).map((question) => ({
        questionId: getQuestionId(question),
        selectedOptionId: Number(drafts?.[getQuestionId(question)] || 0),
    }));
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

function addQuestionDraftRow() {
    courseQuizEditorQuestions.value.push(createEmptyQuestionDraft());
}

function removeQuestionDraftRow(questionIndex) {
    if (courseQuizEditorQuestions.value.length <= 1) {
        return;
    }

    courseQuizEditorQuestions.value.splice(questionIndex, 1);
}

function addOptionToQuestion(questionIndex) {
    if (!courseQuizEditorQuestions.value[questionIndex]) {
        return;
    }

    courseQuizEditorQuestions.value[questionIndex].optionTexts.push('');
}

function removeOptionFromQuestion(questionIndex, optionIndex) {
    const question = courseQuizEditorQuestions.value[questionIndex];
    if (!question || question.optionTexts.length <= 2) {
        return;
    }

    question.optionTexts.splice(optionIndex, 1);
    if (question.correctOptionIndex >= question.optionTexts.length) {
        question.correctOptionIndex = 0;
    }
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

function getDraftMark(student) {
    const studentId = getStudentId(student);
    const rawFromDraft = gradeDraftByStudent.value[studentId];
    if (rawFromDraft !== undefined) {
        return Number(rawFromDraft) || 1;
    }

    const existingMark = Number(student?.mark || student?.Mark);
    return Number.isInteger(existingMark) && existingMark >= 1 && existingMark <= 10 ? existingMark : 1;
}

function sortLessons(rows) {
    return [...rows].sort((a, b) => {
        const orderDiff = getOrderNumber(a) - getOrderNumber(b);
        if (orderDiff !== 0) {
            return orderDiff;
        }

        return String(a?.title || a?.Title || '').localeCompare(String(b?.title || b?.Title || ''));
    });
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
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
        return;
    }

    isLoadingLessons.value = true;

    try {
        const response = await courseApi.getCourseLessons(courseId.value);
        lessons.value = sortLessons(normalizeItems(response));

        const hasSelectedLesson = lessons.value.some((item) => getLessonId(item) === selectedLessonReviewId.value);
        if (!hasSelectedLesson) {
            selectedLessonReviewId.value = getLessonId(lessons.value[0]);
        }
    } catch (error) {
        lessons.value = [];
        errorMessage.value = error?.message || 'Failed to load lessons.';
    } finally {
        isLoadingLessons.value = false;
    }
}

async function loadCourseProgress() {
    if (!courseId.value || !canView.value || canManage.value) {
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

async function saveCourseQuizEditor() {
    if (!canManage.value || !courseId.value || isSavingCourseQuiz.value) {
        return;
    }

    const questionsPayload = courseQuizEditorQuestions.value.map((draft) => buildQuizQuestionPayload(draft));

    if (!questionsPayload.length) {
        errorMessage.value = 'Final quiz must contain at least one question.';
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

        actionMessage.value = readMessage(response) || 'Final quiz saved successfully.';
        closeCourseQuizEditor();
        await loadCourseQuiz();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not save final quiz.';
    } finally {
        isSavingCourseQuiz.value = false;
    }
}

async function loadMyCourseQuizAnswers() {
    if (!canView.value || canManage.value || !courseId.value) {
        myCourseQuizAnswers.value = [];
        courseQuizAnswerDraftByQuestion.value = {};
        return;
    }

    isLoadingMyCourseQuizAnswers.value = true;

    try {
        const response = await courseApi.getMyCourseQuizAnswers(courseId.value);
        myCourseQuizAnswers.value = normalizeAnswerRows(response);

        courseQuizAnswerDraftByQuestion.value = courseQuizQuestions.value.reduce((acc, item) => {
            const questionIdValue = getQuestionId(item);
            acc[questionIdValue] = getExistingAnswerOptionId(questionIdValue, myCourseQuizAnswers.value);
            return acc;
        }, {});
    } catch {
        myCourseQuizAnswers.value = [];
        courseQuizAnswerDraftByQuestion.value = {};
    } finally {
        isLoadingMyCourseQuizAnswers.value = false;
    }
}

async function loadTeacherInsights() {
    if (!canView.value || !canManage.value || !courseId.value) {
        studentsProgress.value = [];
        teacherCourseQuizAnswers.value = [];
        teacherLessonQuizAnswers.value = [];
        gradeDraftByStudent.value = {};
        return;
    }

    isLoadingTeacherInsights.value = true;

    try {
        const [studentsResponse, courseAnswersResponse] = await Promise.all([
            courseApi.getCourseStudentsProgress(courseId.value),
            courseApi.getCourseQuizAnswersForTeacher(courseId.value),
        ]);

        studentsProgress.value = normalizeItems(studentsResponse);
        teacherCourseQuizAnswers.value = normalizeAnswerRows(courseAnswersResponse);

        const hasSelectedStudent = studentsProgress.value.some((item) => getStudentId(item) === selectedTeacherStudentId.value);
        selectedTeacherStudentId.value = hasSelectedStudent ? selectedTeacherStudentId.value : getStudentId(studentsProgress.value[0]);

        gradeDraftByStudent.value = studentsProgress.value.reduce((acc, item) => {
            const studentId = getStudentId(item);
            if (!studentId) {
                return acc;
            }

            acc[studentId] = getDraftMark(item);
            return acc;
        }, {});
    } catch (error) {
        studentsProgress.value = [];
        teacherCourseQuizAnswers.value = [];
        gradeDraftByStudent.value = {};
        errorMessage.value = error?.message || 'Failed to load teacher results.';
    } finally {
        isLoadingTeacherInsights.value = false;
    }
}

async function loadSelectedLessonTeacherAnswers() {
    if (!canManage.value || !selectedLessonReviewId.value) {
        teacherLessonQuizAnswers.value = [];
        return;
    }

    isLoadingLessonAnswers.value = true;
    try {
        const response = await courseApi.getLessonQuizAnswersForTeacher(selectedLessonReviewId.value);
        teacherLessonQuizAnswers.value = normalizeAnswerRows(response);
    } catch {
        teacherLessonQuizAnswers.value = [];
    } finally {
        isLoadingLessonAnswers.value = false;
    }
}

async function refreshWorkspace() {
    await loadUniversityMembership();

    if (canView.value) {
        await loadCourse();
        await loadLessons();
        await loadCourseProgress();
        await loadCourseQuiz();
        await loadMyCourseQuizAnswers();
        await loadTeacherInsights();
        await loadSelectedLessonTeacherAnswers();
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
        await loadCourseQuiz();
        await loadMyCourseQuizAnswers();
        await loadTeacherInsights();
        await loadSelectedLessonTeacherAnswers();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not process university access action.';
    } finally {
        isJoining.value = false;
    }
}

async function submitCourseQuiz() {
    if (!courseId.value || isSubmittingCourseQuiz.value || !canEditCourseQuizAnswers()) {
        return;
    }

    if (!hasSelectedAnswersForAllQuestions(courseQuizQuestions.value, courseQuizAnswerDraftByQuestion.value)) {
        errorMessage.value = 'Please select answers for all final quiz questions before submitting.';
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

        actionMessage.value = readMessage(response) || 'Final quiz submitted.';
        await loadMyCourseQuizAnswers();
        await loadCourseProgress();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not submit final quiz.';
    } finally {
        isSubmittingCourseQuiz.value = false;
    }
}

function openCoursesWorkspace() {
    router.push({ name: 'UniversityCourses', params: { name: universityName.value } });
}

function openLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonDetail',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId,
        },
    });
}

function openEditLesson(lesson) {
    const lessonId = getLessonId(lesson);
    if (!lessonId) {
        return;
    }

    router.push({
        name: 'UniversityCourseLessonEdit',
        params: {
            name: universityName.value,
            courseId: courseId.value,
            lessonId,
        },
    });
}

function openCreateLesson() {
    router.push({
        name: 'UniversityCourseLessonCreate',
        params: {
            name: universityName.value,
            courseId: courseId.value,
        },
    });
}

function openTeacherStudentResults(student) {
    selectedTeacherStudentId.value = getStudentId(student);
    teacherViewMode.value = 'student';
}

function closeTeacherStudentResults() {
    teacherViewMode.value = 'list';
}

async function onLessonReviewChange() {
    await loadSelectedLessonTeacherAnswers();
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
        errorMessage.value = 'Mark can be assigned only after completed lessons and quizzes.';
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
        await loadTeacherInsights();
    } catch (error) {
        errorMessage.value = error?.message || 'Could not save student mark.';
    } finally {
        isSavingGrade.value = false;
    }
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
            subtitle="Course lessons"
            :is-opened="Boolean(university?.isOpened)"
            :is-member="isMember"
            :join-visible="joinVisible"
            :join-label="joinLabel"
            :is-joining="isJoining"
            :join-disabled="isLoadingWorkspace"
            @join="joinUniversity"
        />

        <section class="surface-card workspace-card">
            <div class="workspace-card__head">
                <div>
                    <p class="section-kicker">Lessons</p>
                    <h2 class="section-title">{{ getCourseTitle() }}</h2>
                </div>

                <div class="workspace-card__actions">
                    <div class="workspace-card__nav-actions">
                        <button class="secondary-button icon-action" type="button" title="Back to courses" @click="openCoursesWorkspace">
                            <AppIcon name="back" :boxed="false" />
                        </button>
                        <button class="secondary-button icon-action" type="button" title="Refresh" :disabled="isLoadingWorkspace || isLoadingLessons" @click="refreshWorkspace">
                            <AppIcon name="refresh" :boxed="false" />
                        </button>
                    </div>

                    <button v-if="canManage" class="submit-button" type="button" :disabled="isLoadingLessons" @click="openCreateLesson">
                        New lesson
                    </button>
                </div>
            </div>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <article v-if="canView && !canManage" class="surface-card progress-card">
                <div class="progress-card__head">
                    <p class="section-kicker">Progress</p>
                    <p class="progress-card__status">{{ progressCompletedCount }} / {{ progressTotalCount }} done</p>
                </div>

                <div class="progress-card__track" role="progressbar" :aria-valuenow="progressPercent" aria-valuemin="0" aria-valuemax="100">
                    <div class="progress-card__fill" :style="{ width: `${progressPercent}%` }" />
                </div>
            </article>

            <section v-if="canView && !canManage && isFinalQuizUnlocked" class="surface-card final-quiz-panel">
                <div class="final-quiz-panel__head">
                    <p class="section-kicker">Final quiz</p>
                    <p v-if="hasCourseQuizQuestions" class="final-quiz-panel__policy">
                        Policy: {{ isSingleAttemptCourseQuiz() ? 'Single attempt' : 'Reattempt allowed' }}
                    </p>
                </div>

                <p v-if="isLoadingCourseQuiz || isLoadingMyCourseQuizAnswers" class="state-message">Loading final quiz...</p>

                <div v-else-if="hasCourseQuizQuestions" class="final-quiz-questions">
                    <article v-for="question in courseQuizQuestions" :key="getQuestionId(question)" class="final-quiz-question">
                        <p class="final-quiz-question__text">{{ question.questionText || question.QuestionText }}</p>

                        <label
                            v-for="option in question.options || question.Options || []"
                            :key="getOptionId(option)"
                            class="final-quiz-question__option"
                        >
                            <input
                                v-model.number="courseQuizAnswerDraftByQuestion[getQuestionId(question)]"
                                :disabled="!canEditCourseQuizAnswers() || isSubmittingCourseQuiz"
                                type="radio"
                                :name="`course-question-${getQuestionId(question)}`"
                                :value="getOptionId(option)"
                            />
                            <span>{{ option.optionText || option.OptionText }}</span>
                        </label>
                    </article>

                    <button
                        class="submit-button"
                        type="button"
                        :disabled="isSubmittingCourseQuiz || !hasSelectedAnswersForAllQuestions(courseQuizQuestions, courseQuizAnswerDraftByQuestion) || !canEditCourseQuizAnswers()"
                        @click="submitCourseQuiz"
                    >
                        {{ isSubmittingCourseQuiz ? 'Submitting...' : 'Submit final quiz' }}
                    </button>

                    <p v-if="isSingleAttemptCourseQuiz() && myCourseQuizAnswers.length" class="state-message final-quiz-panel__lock">
                        Final quiz is locked after your first submit.
                    </p>

                    <p v-if="myCourseQuizAnswers.length" class="final-quiz-panel__score">Your score: {{ myCourseQuizScoreLabel }}</p>
                </div>

                <p v-else class="state-message">No final quiz questions published yet.</p>
            </section>

            <article v-else-if="canView && !canManage" class="surface-card final-quiz-panel">
                <div class="final-quiz-panel__head">
                    <p class="section-kicker">Final quiz</p>
                </div>
                <p class="state-message">Final quiz unlocks after all lessons are marked as finished.</p>
            </article>

            <div v-if="canView" class="lessons-list">
                <LessonListMobileItem
                    v-for="lesson in lessons"
                    :key="getLessonId(lesson)"
                    :lesson="lesson"
                    :can-manage="canManage"
                    :is-busy="isLoadingLessons"
                    @open="openLesson"
                    @edit="openEditLesson"
                />

                <p v-if="!isLoadingLessons && !hasLessons" class="state-message">No lessons yet.</p>
            </div>

            <section v-if="canManage && canView" class="surface-card teacher-panel">
                <div class="teacher-panel__head">
                    <div>
                        <p class="section-kicker">Teacher results</p>
                        <h3>Students and detailed progress review</h3>
                    </div>
                </div>

                <p v-if="isLoadingTeacherInsights" class="state-message">Loading teacher results...</p>

                <div v-else-if="studentsProgress.length" class="teacher-panel__content">
                    <section class="surface-card teacher-box">
                        <div class="teacher-box__head">
                            <p class="section-kicker">Final quiz editor</p>
                            <div class="teacher-box__actions">
                                <button
                                    class="secondary-button"
                                    type="button"
                                    :disabled="isSavingCourseQuiz"
                                    @click="hasCourseQuizQuestions ? openEditCourseQuizEditor() : openCreateCourseQuizEditor()"
                                >
                                    {{ hasCourseQuizQuestions ? 'Edit final quiz' : 'Create final quiz' }}
                                </button>
                            </div>
                        </div>

                        <div v-if="isCourseQuizEditorOpen" class="quiz-editor">
                            <div class="field-group">
                                <label class="field-label">Attempt policy for whole final quiz</label>
                                <select v-model="courseQuizEditorAttemptPolicy" class="input-field">
                                    <option value="reattempt">Reattempt allowed</option>
                                    <option value="single">Single attempt (lock after first submit)</option>
                                </select>
                            </div>

                            <div class="quiz-editor__questions">
                                <article
                                    v-for="(questionDraft, questionIndex) in courseQuizEditorQuestions"
                                    :key="`teacher-course-question-draft-${questionIndex}`"
                                    class="quiz-editor__question-card"
                                >
                                    <div class="field-group">
                                        <label class="field-label">Question {{ questionIndex + 1 }}</label>
                                        <input
                                            v-model="questionDraft.questionText"
                                            class="input-field"
                                            type="text"
                                            maxlength="400"
                                            placeholder="What does HTTP status 404 mean?"
                                        />
                                    </div>

                                    <div class="quiz-editor__options">
                                        <div
                                            v-for="(optionText, optionIndex) in questionDraft.optionTexts"
                                            :key="`teacher-course-opt-${questionIndex}-${optionIndex}`"
                                            class="quiz-editor__option-row"
                                        >
                                            <input
                                                v-model="questionDraft.optionTexts[optionIndex]"
                                                class="input-field"
                                                type="text"
                                                maxlength="300"
                                                :placeholder="`Variant ${optionIndex + 1}`"
                                            />
                                            <label class="quiz-editor__correct">
                                                <input
                                                    v-model.number="questionDraft.correctOptionIndex"
                                                    type="radio"
                                                    :name="`teacher-course-quiz-correct-${questionIndex}`"
                                                    :value="optionIndex"
                                                />
                                                Correct
                                            </label>
                                            <button
                                                class="secondary-button"
                                                type="button"
                                                :disabled="questionDraft.optionTexts.length <= 2"
                                                @click="removeOptionFromQuestion(questionIndex, optionIndex)"
                                            >
                                                Remove
                                            </button>
                                        </div>
                                    </div>

                                    <div class="teacher-box__actions">
                                        <button class="secondary-button" type="button" @click="addOptionToQuestion(questionIndex)">Add variant</button>
                                        <button
                                            class="secondary-button"
                                            type="button"
                                            :disabled="courseQuizEditorQuestions.length <= 1"
                                            @click="removeQuestionDraftRow(questionIndex)"
                                        >
                                            Remove question
                                        </button>
                                    </div>
                                </article>
                            </div>

                            <div class="teacher-box__actions">
                                <button class="secondary-button" type="button" @click="addQuestionDraftRow">Add question</button>
                                <button class="submit-button" type="button" :disabled="isSavingCourseQuiz" @click="saveCourseQuizEditor">
                                    {{ isSavingCourseQuiz ? 'Saving...' : 'Save final quiz' }}
                                </button>
                                <button class="secondary-button" type="button" :disabled="isSavingCourseQuiz" @click="closeCourseQuizEditor">Cancel</button>
                            </div>
                        </div>
                    </section>

                    <section v-if="teacherViewMode === 'list'" class="surface-card teacher-box teacher-students-view">
                        <div class="teacher-box__head">
                            <p class="section-kicker">Students</p>
                            <p class="state-message">Search by nickname using typo-friendly matching.</p>
                        </div>

                        <div class="field-group">
                            <label class="field-label" for="teacher-student-search">Find student</label>
                            <input
                                id="teacher-student-search"
                                v-model="teacherStudentSearchQuery"
                                class="input-field"
                                type="text"
                                maxlength="100"
                                placeholder="Start typing nickname..."
                            />
                        </div>

                        <div class="teacher-panel__students">
                            <button
                                v-for="student in filteredStudentsProgress"
                                :key="getStudentId(student)"
                                class="teacher-student-item"
                                :class="{ 'teacher-student-item--active': selectedTeacherStudentId === getStudentId(student) }"
                                type="button"
                                @click="openTeacherStudentResults(student)"
                            >
                                <p class="teacher-student-item__name">{{ getStudentName(student) }}</p>
                                <p class="teacher-student-item__meta">
                                    Lessons: {{ student.completedLessons || student.CompletedLessons || 0 }} / {{ student.totalLessons || student.TotalLessons || 0 }}
                                </p>
                                <p class="teacher-student-item__meta">
                                    Final quiz: {{ student.completedQuizQuestions || student.CompletedQuizQuestions || 0 }} / {{ student.totalQuizQuestions || student.TotalQuizQuestions || 0 }}
                                </p>
                            </button>

                            <p v-if="!filteredStudentsProgress.length" class="state-message">No students found for current search.</p>
                        </div>
                    </section>

                    <div v-else-if="selectedTeacherStudent" class="teacher-panel__details">
                        <section class="surface-card teacher-box teacher-student-details-head">
                            <div class="teacher-box__head">
                                <p class="section-kicker">Student result workspace</p>
                                <h3>{{ getStudentName(selectedTeacherStudent) }}</h3>
                                <div class="teacher-box__actions">
                                    <button class="secondary-button" type="button" @click="closeTeacherStudentResults">Back to student list</button>
                                </div>
                            </div>
                        </section>

                        <section class="surface-card teacher-box">
                            <p class="section-kicker">Marking</p>
                            <div class="teacher-box__row">
                                <select
                                    class="input-field"
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
                            <p v-if="!selectedTeacherStudentCanBeMarked" class="state-message">Student must finish lessons and quizzes before marking.</p>
                        </section>

                        <section class="surface-card teacher-box">
                            <div class="teacher-box__head">
                                <p class="section-kicker">Lesson quiz results</p>
                                <select v-model.number="selectedLessonReviewId" class="input-field teacher-box__select" @change="onLessonReviewChange">
                                    <option v-for="lesson in lessons" :key="getLessonId(lesson)" :value="getLessonId(lesson)">
                                        {{ lesson.title || lesson.Title }}
                                    </option>
                                </select>
                            </div>

                            <p v-if="isLoadingLessonAnswers" class="state-message">Loading lesson results...</p>

                            <div v-else-if="selectedTeacherStudentLessonQuizAnswers.length" class="teacher-answers">
                                <article v-for="(answer, index) in selectedTeacherStudentLessonQuizAnswers" :key="`lesson-answer-${index}`" class="teacher-answer-item">
                                    <p>{{ answer.questionText || answer.QuestionText }}</p>
                                    <p>Selected: {{ answer.selectedOptionText || answer.SelectedOptionText }}</p>
                                    <p>Correct: {{ answer.correctOptionText || answer.CorrectOptionText }}</p>
                                    <span class="pill" :class="(answer.isCorrect || answer.IsCorrect) ? 'pill--accent' : 'pill--pink'">
                                        {{ (answer.isCorrect || answer.IsCorrect) ? 'Correct' : 'Incorrect' }}
                                    </span>
                                </article>
                            </div>
                            <p v-else class="state-message">No lesson quiz answers for this student yet.</p>
                        </section>

                        <section class="surface-card teacher-box">
                            <p class="section-kicker">Final course quiz</p>
                            <div v-if="selectedTeacherStudentCourseQuizAnswers.length" class="teacher-answers">
                                <article v-for="(answer, index) in selectedTeacherStudentCourseQuizAnswers" :key="`course-answer-${index}`" class="teacher-answer-item">
                                    <p>{{ answer.questionText || answer.QuestionText }}</p>
                                    <p>Selected: {{ answer.selectedOptionText || answer.SelectedOptionText }}</p>
                                    <p>Correct: {{ answer.correctOptionText || answer.CorrectOptionText }}</p>
                                    <span class="pill" :class="(answer.isCorrect || answer.IsCorrect) ? 'pill--accent' : 'pill--pink'">
                                        {{ (answer.isCorrect || answer.IsCorrect) ? 'Correct' : 'Incorrect' }}
                                    </span>
                                </article>
                            </div>
                            <p v-else class="state-message">No final quiz answers for this student yet.</p>
                        </section>
                    </div>

                    <div v-else class="teacher-panel__details">
                        <p class="state-message">Select a student to review detailed results.</p>
                    </div>
                </div>

                <p v-else class="state-message">Student activity will appear here when learners start progress.</p>
            </section>

            <p v-else-if="!canView" class="state-message">Membership is required to view lessons in this course.</p>
        </section>
    </main>
</template>

<style scoped>
.page-shell {
    width: min(760px, calc(100% - 1.2rem));
    margin: 1rem auto 1.4rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.workspace-card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.8rem;
}

.workspace-card__head {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.workspace-card__actions {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.55rem;
}

.workspace-card__nav-actions {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
}

.workspace-card__actions .submit-button {
    min-height: 2.6rem;
}

.icon-action {
    min-height: 2.6rem;
    min-width: 2.8rem;
    padding: 0.45rem;
    border-radius: 0.8rem;
}

.icon-action :deep(.app-icon) {
    width: auto;
    height: auto;
    border-radius: 0;
}

.lessons-list {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.progress-card {
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    background: linear-gradient(180deg, rgba(143, 44, 226, 0.06), rgba(255, 95, 162, 0.08));
}

.progress-card__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
}

.progress-card__status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
}

.progress-card__track {
    height: 0.62rem;
    border-radius: 0.6rem;
    background: rgba(143, 44, 226, 0.14);
    overflow: hidden;
}

.progress-card__fill {
    height: 100%;
    border-radius: 0.6rem;
    background: linear-gradient(90deg, rgba(143, 44, 226, 0.85), rgba(255, 95, 162, 0.85));
    transition: width var(--ttl-transition-base);
}

.final-quiz-panel {
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.final-quiz-panel__head {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.final-quiz-panel__policy {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
}

.final-quiz-questions {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.final-quiz-question {
    padding: 0.55rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-surface-soft);
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.final-quiz-question__text {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.final-quiz-question__option {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    color: var(--ttl-text-secondary);
}

.final-quiz-panel__lock {
    color: var(--ttl-warning);
}

.final-quiz-panel__score {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 800;
}

.teacher-panel {
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.teacher-panel__head h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 1.05rem;
}

.teacher-panel__content {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.teacher-students-view {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.teacher-panel__students {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.teacher-student-item {
    text-align: left;
    padding: 0.55rem;
    border: 1px solid var(--ttl-border-subtle);
    border-radius: var(--ttl-radius-sm);
    background: var(--ttl-bg-surface-soft);
    cursor: pointer;
}

.teacher-student-item--active {
    border-color: rgba(143, 44, 226, 0.35);
    background: rgba(143, 44, 226, 0.09);
}

.teacher-student-item__name {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.teacher-student-item__meta {
    margin: 0.2rem 0 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
}

.teacher-panel__details {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.teacher-student-details-head h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    font-size: 1.05rem;
}

.teacher-box {
    padding: 0.7rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.teacher-box__row {
    display: grid;
    grid-template-columns: minmax(0, 120px) auto;
    gap: 0.55rem;
    align-items: center;
}

.teacher-box__head {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.teacher-box__actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.teacher-box__select {
    min-height: 2.5rem;
}

.teacher-answers {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.teacher-answer-item {
    padding: 0.5rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    background: var(--ttl-bg-surface-soft);
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}

.teacher-answer-item p {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.83rem;
}

.quiz-editor {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
    padding: 0.6rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    background: var(--ttl-bg-surface-soft);
}

.quiz-editor__questions {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.quiz-editor__question-card {
    padding: 0.55rem;
    border-radius: var(--ttl-radius-sm);
    border: 1px solid var(--ttl-border-subtle);
    background: var(--ttl-bg-page);
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.quiz-editor__options {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
}

.quiz-editor__option-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    gap: 0.5rem;
    align-items: center;
}

.quiz-editor__correct {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    color: var(--ttl-text-secondary);
    font-size: 0.84rem;
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

@media (max-width: 620px) {
    .workspace-card__actions {
        flex-direction: column;
        align-items: stretch;
    }

    .workspace-card__nav-actions {
        width: 100%;
    }

    .workspace-card__nav-actions .icon-action {
        flex: 1 1 auto;
    }

    .teacher-box__row {
        grid-template-columns: 1fr;
    }

    .quiz-editor__option-row {
        grid-template-columns: 1fr;
    }
}
</style>
