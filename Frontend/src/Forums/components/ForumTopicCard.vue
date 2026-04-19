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
const isLoadingMoreComments = ref(false);
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

async function loadMoreComments() {
    if (!hasMoreComments.value || isLoadingComments.value || isLoadingMoreComments.value) {
        return;
    }

    isLoadingMoreComments.value = true;

    try {
        const nextPage = commentsPage.value + 1;
        const response = await forumApi.getComments({
            isTopic: true,
            recordId: localTopic.value.id,
            page: nextPage,
        });

        const values = normalizeItems(response);
        comments.value = [...comments.value, ...values];
        commentsPage.value = nextPage;
        hasMoreComments.value = values.length === 10;
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load more comments.';
    } finally {
        isLoadingMoreComments.value = false;
    }
}

async function toggleComments() {
    isCommentsOpen.value = !isCommentsOpen.value;

    if (isCommentsOpen.value && !comments.value.length) {
        await loadComments({ reset: true });
    }
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
    <article class="topic-card surface-card">
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

        <footer class="topic-card__footer" aria-label="Topic actions">
            <button class="action-chip" type="button" title="Like" aria-label="Like topic" @click="likeTopic" :disabled="isLiking || isDisliking">
                <AppIcon name="vote-up" :boxed="false" />
                <span class="action-chip__count">{{ localTopic.likes }}</span>
            </button>
            <button class="action-chip" type="button" title="Dislike" aria-label="Dislike topic" @click="dislikeTopic" :disabled="isLiking || isDisliking">
                <AppIcon name="vote-down" :boxed="false" />
                <span class="action-chip__count">{{ localTopic.dislikes }}</span>
            </button>
            <button class="action-chip" type="button" title="Toggle comments" aria-label="Toggle comments" @click="toggleComments" :disabled="isLoadingComments">
                <AppIcon name="comment" :boxed="false" />
            </button>
            <button class="action-chip" type="button" title="Reply" aria-label="Write comment" @click="toggleCommentForm" :disabled="isCreatingComment">
                <AppIcon :name="isCommentFormOpen ? 'chevron-down' : 'reply'" :boxed="false" />
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
            <button class="action-chip action-chip--send" type="submit" title="Send" aria-label="Send comment" :disabled="isCreatingComment || !commentText.trim()">
                <AppIcon name="send" :boxed="false" />
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

            <footer v-if="hasMoreComments" class="topic-card__more-row">
                <button class="action-chip" type="button" title="Load more comments" aria-label="Load more comments" @click="loadMoreComments" :disabled="isLoadingMoreComments">
                    <AppIcon name="comment" :boxed="false" />
                    <span>{{ isLoadingMoreComments ? 'Loading' : 'More' }}</span>
                </button>
            </footer>
        </section>
    </article>
</template>

<style scoped>
.topic-card {
    padding: 0.8rem 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
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
    font-size: 1rem;
}

.topic-card__content {
    margin: 0;
    color: var(--ttl-text-primary);
    line-height: 1.5;
    white-space: pre-wrap;
    word-break: break-word;
}

.topic-card__footer {
    display: flex;
    align-items: center;
    gap: 0.45rem;
}

.action-chip {
    min-height: 2rem;
    border: 1px solid rgba(143, 44, 226, 0.16);
    border-radius: 999px;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.9), rgba(143, 44, 226, 0.06));
    color: var(--ttl-accent-dark);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.3rem;
    padding: 0 0.58rem;
    cursor: pointer;
    transition: transform var(--ttl-transition-fast), border-color var(--ttl-transition-base), background var(--ttl-transition-base);
}

.action-chip:hover {
    transform: translateY(-1px);
    border-color: rgba(143, 44, 226, 0.32);
}

.action-chip:disabled {
    opacity: 0.6;
    cursor: default;
}

.action-chip--send {
    align-self: flex-end;
}

.action-chip__count {
    min-width: 1rem;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
    text-align: center;
}

.topic-card__comment-form {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.topic-card__textarea {
    resize: vertical;
}

.topic-card__comments {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
    border-top: 1px solid rgba(143, 44, 226, 0.12);
    padding-top: 0.45rem;
}

.topic-card__pagination {
    display: none;
}

.topic-card__more-row {
    display: flex;
    justify-content: flex-start;
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

    .topic-card__footer {
        flex-wrap: wrap;
    }
}
</style>
