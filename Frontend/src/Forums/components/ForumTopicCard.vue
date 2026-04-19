<script setup>
import { computed, ref } from 'vue';
import forumApi from '@/Forums/services/forumApi';
import AppIcon from '@/Shared/components/AppIcon.vue';
import ForumCommentNode from '@/Forums/components/ForumCommentNode.vue';

const props = defineProps({
    topic: {
        type: Object,
        required: true,
    },
    universityName: {
        type: String,
        required: true,
    },
});

const comments = ref([]);
const isCommentsOpen = ref(false);
const isLoadingComments = ref(false);
const commentsPage = ref(0);
const hasMoreComments = ref(false);
const isCommentFormOpen = ref(false);
const commentText = ref('');
const isCreatingComment = ref(false);
const isLiking = ref(false);
const isDisliking = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');

const localTopic = ref({
    id: Number(props.topic.id || props.topic.Id || 0),
    topicTitle: String(props.topic.topicTitle || props.topic.TopicTitle || ''),
    topicContent: String(props.topic.topicContent || props.topic.TopicContent || ''),
    creatorName: String(props.topic.creatorName || props.topic.CreatorName || 'Unknown user'),
    creatorRole: String(props.topic.creatorRole || props.topic.CreatorRole || 'Unknown'),
    createdAt: props.topic.createdAt || props.topic.CreatedAt || null,
    likes: Number(props.topic.likes || props.topic.Likes || 0),
    dislikes: Number(props.topic.dislikes || props.topic.Dislikes || 0),
});

const roleBadgeClass = computed(() => {
    const normalized = localTopic.value.creatorRole.toLowerCase();

    if (normalized === 'manager') {
        return 'pill pill--accent';
    }

    if (normalized === 'teacher') {
        return 'pill';
    }

    return 'pill pill--pink';
});

const createdAtLabel = computed(() => {
    const raw = localTopic.value.createdAt;
    if (!raw) {
        return 'Unknown date';
    }

    const parsed = new Date(raw);
    if (Number.isNaN(parsed.getTime())) {
        return 'Unknown date';
    }

    return parsed.toLocaleString();
});

function normalizeItems(payload) {
    return payload?.values || payload?.Values || payload?.items || payload?.Items || [];
}

function readMessage(payload) {
    return payload?.message || payload?.Message || '';
}

function clearMessages() {
    actionMessage.value = '';
    errorMessage.value = '';
}

async function loadComments({ reset = false } = {}) {
    if (isLoadingComments.value || !localTopic.value.id) {
        return;
    }

    if (reset) {
        commentsPage.value = 0;
    }

    clearMessages();
    isLoadingComments.value = true;

    try {
        const response = await forumApi.getComments({
            isTopic: true,
            recordId: localTopic.value.id,
            page: commentsPage.value,
        });

        const values = normalizeItems(response);
        comments.value = values;
        hasMoreComments.value = values.length === 10;
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load comments.';
        comments.value = [];
        hasMoreComments.value = false;
    } finally {
        isLoadingComments.value = false;
    }
}

async function toggleComments() {
    isCommentsOpen.value = !isCommentsOpen.value;

    if (isCommentsOpen.value && !comments.value.length) {
        await loadComments({ reset: true });
    }
}

async function goToPrevCommentPage() {
    if (commentsPage.value <= 0 || isLoadingComments.value) {
        return;
    }

    commentsPage.value -= 1;
    await loadComments();
}

async function goToNextCommentPage() {
    if (!hasMoreComments.value || isLoadingComments.value) {
        return;
    }

    commentsPage.value += 1;
    await loadComments();
}

function toggleCommentForm() {
    isCommentFormOpen.value = !isCommentFormOpen.value;
    if (!isCommentFormOpen.value) {
        commentText.value = '';
    }
}

async function createComment() {
    if (!commentText.value.trim() || isCreatingComment.value) {
        return;
    }

    clearMessages();
    isCreatingComment.value = true;

    try {
        const response = await forumApi.createComment({
            postId: localTopic.value.id,
            isTopic: true,
            universityName: props.universityName,
            commentContent: commentText.value.trim(),
        });

        actionMessage.value = readMessage(response) || 'Comment created.';
        commentText.value = '';
        isCommentFormOpen.value = false;

        if (isCommentsOpen.value) {
            await loadComments({ reset: true });
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not create comment.';
    } finally {
        isCreatingComment.value = false;
    }
}

async function likeTopic() {
    if (isLiking.value) {
        return;
    }

    clearMessages();
    isLiking.value = true;

    try {
        const response = await forumApi.likeTopic(localTopic.value.id);
        const message = readMessage(response);
        actionMessage.value = message || 'Topic reaction updated.';

        if (message.toLowerCase().includes('removed')) {
            localTopic.value.likes = Math.max(0, localTopic.value.likes - 1);
        } else {
            localTopic.value.likes += 1;
            if (localTopic.value.dislikes > 0 && message.toLowerCase().includes('liked')) {
                localTopic.value.dislikes -= 1;
            }
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not update like.';
    } finally {
        isLiking.value = false;
    }
}

async function dislikeTopic() {
    if (isDisliking.value) {
        return;
    }

    clearMessages();
    isDisliking.value = true;

    try {
        const response = await forumApi.dislikeTopic(localTopic.value.id);
        const message = readMessage(response);
        actionMessage.value = message || 'Topic reaction updated.';

        if (message.toLowerCase().includes('removed')) {
            localTopic.value.dislikes = Math.max(0, localTopic.value.dislikes - 1);
        } else {
            localTopic.value.dislikes += 1;
            if (localTopic.value.likes > 0 && message.toLowerCase().includes('disliked')) {
                localTopic.value.likes -= 1;
            }
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not update dislike.';
    } finally {
        isDisliking.value = false;
    }
}
</script>

<template>
    <article class="topic-card surface-card hover-lift">
        <header class="topic-card__header">
            <div class="topic-card__author">
                <AppIcon name="profile" :boxed="false" />
                <p>{{ localTopic.creatorName }}</p>
                <span :class="roleBadgeClass">{{ localTopic.creatorRole }}</span>
            </div>
            <p class="topic-card__time">{{ createdAtLabel }}</p>
        </header>

        <h3>{{ localTopic.topicTitle }}</h3>
        <p class="topic-card__content">{{ localTopic.topicContent }}</p>

        <footer class="topic-card__footer">
            <button class="secondary-button" type="button" @click="likeTopic" :disabled="isLiking || isDisliking">
                Like {{ localTopic.likes }}
            </button>
            <button class="secondary-button" type="button" @click="dislikeTopic" :disabled="isLiking || isDisliking">
                Dislike {{ localTopic.dislikes }}
            </button>
            <button class="secondary-button" type="button" @click="toggleComments" :disabled="isLoadingComments">
                {{ isCommentsOpen ? 'Hide comments' : 'Show comments' }}
            </button>
            <button class="secondary-button" type="button" @click="toggleCommentForm" :disabled="isCreatingComment">
                {{ isCommentFormOpen ? 'Cancel' : 'Add comment' }}
            </button>
        </footer>

        <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
        <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

        <form v-if="isCommentFormOpen" class="topic-card__comment-form" @submit.prevent="createComment">
            <textarea
                v-model="commentText"
                class="input-field topic-card__textarea"
                rows="4"
                maxlength="1000"
                placeholder="Write your comment"
            />
            <button class="submit-button" type="submit" :disabled="isCreatingComment || !commentText.trim()">
                {{ isCreatingComment ? 'Posting...' : 'Post comment' }}
            </button>
        </form>

        <section v-if="isCommentsOpen" class="topic-card__comments">
            <p v-if="isLoadingComments" class="state-message">Loading comments...</p>
            <p v-else-if="!comments.length" class="state-message">No comments on this topic yet.</p>

            <ForumCommentNode
                v-for="comment in comments"
                :key="comment.id || comment.Id"
                :comment="comment"
                :university-name="universityName"
                :depth="0"
            />

            <footer class="topic-card__pagination">
                <button class="secondary-button" type="button" @click="goToPrevCommentPage" :disabled="commentsPage <= 0 || isLoadingComments">
                    Previous comments
                </button>
                <p class="pagination-status">Page {{ commentsPage + 1 }}</p>
                <button class="secondary-button" type="button" @click="goToNextCommentPage" :disabled="!hasMoreComments || isLoadingComments">
                    Next comments
                </button>
            </footer>
        </section>
    </article>
</template>

<style scoped>
.topic-card {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.topic-card__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.8rem;
}

.topic-card__author {
    display: inline-flex;
    align-items: center;
    gap: 0.55rem;
}

.topic-card__author p {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.topic-card__time {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.85rem;
}

.topic-card h3 {
    margin: 0;
    color: var(--ttl-text-primary);
    letter-spacing: -0.02em;
}

.topic-card__content {
    margin: 0;
    color: var(--ttl-text-primary);
    line-height: 1.6;
    white-space: pre-wrap;
    word-break: break-word;
}

.topic-card__footer {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.topic-card__comment-form {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.topic-card__textarea {
    resize: vertical;
}

.topic-card__comments {
    display: flex;
    flex-direction: column;
    gap: 0.7rem;
}

.topic-card__pagination {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.7rem;
}

.pagination-status {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.86rem;
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

@media (max-width: 700px) {
    .topic-card__header {
        flex-direction: column;
        align-items: flex-start;
    }

    .topic-card__pagination {
        flex-direction: column;
        align-items: stretch;
    }

    .topic-card__pagination .secondary-button {
        width: 100%;
    }
}
</style>
