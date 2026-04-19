<script setup>
import { computed, ref } from 'vue';
import forumApi from '@/Forums/services/forumApi';
import AppIcon from '@/Shared/components/AppIcon.vue';

defineOptions({
    name: 'ForumCommentNode',
});

const props = defineProps({
    comment: {
        type: Object,
        required: true,
    },
    universityName: {
        type: String,
        required: true,
    },
    depth: {
        type: Number,
        default: 0,
    },
});

const replies = ref([]);
const isRepliesOpen = ref(false);
const isLoadingReplies = ref(false);
const replyPage = ref(0);
const hasMoreReplies = ref(false);
const isLoadingMoreReplies = ref(false);
const isReplyFormOpen = ref(false);
const replyText = ref('');
const isCreatingReply = ref(false);
const actionMessage = ref('');
const errorMessage = ref('');
const isLiking = ref(false);
const isDisliking = ref(false);

const localComment = ref({
    id: Number(props.comment.id || props.comment.Id || 0),
    postId: Number(props.comment.postId || props.comment.PostId || 0),
    isTopic: Boolean(props.comment.isTopic ?? props.comment.IsTopic),
    creatorName: String(props.comment.creatorName || props.comment.CreatorName || 'Unknown user'),
    creatorRole: String(props.comment.creatorRole || props.comment.CreatorRole || 'Unknown'),
    commentContent: String(props.comment.commentContent || props.comment.CommentContent || ''),
    likesOverall: Number(props.comment.likesOverall || props.comment.LikesOverall || 0),
    dislikesOverall: Number(props.comment.dislikesOverall || props.comment.DislikesOverall || 0),
    repliesCount: Number(props.comment.repliesCount || props.comment.RepliesCount || 0),
    createdAt: props.comment.createdAt || props.comment.CreatedAt || null,
});

const paddingStyle = computed(() => {
    const size = Math.min(props.depth, 5) * 0.7;
    return { paddingLeft: `${size}rem` };
});

const roleBadgeClass = computed(() => {
    const normalized = localComment.value.creatorRole.toLowerCase();
    if (normalized === 'manager') {
        return 'pill pill--accent';
    }

    if (normalized === 'teacher') {
        return 'pill';
    }

    return 'pill pill--pink';
});

const hasReplies = computed(() => localComment.value.repliesCount > 0);

const createdAtLabel = computed(() => {
    const raw = localComment.value.createdAt;
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

async function loadReplies({ reset = false } = {}) {
    if (isLoadingReplies.value || !localComment.value.id) {
        return;
    }

    if (reset) {
        replyPage.value = 0;
    }

    clearMessages();
    isLoadingReplies.value = true;

    try {
        const response = await forumApi.getComments({
            isTopic: false,
            recordId: localComment.value.id,
            page: replyPage.value,
        });

        const values = normalizeItems(response);

        replies.value = values;
        hasMoreReplies.value = values.length === 5;
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load replies.';
        replies.value = [];
        hasMoreReplies.value = false;
    } finally {
        isLoadingReplies.value = false;
    }
}

async function loadMoreReplies() {
    if (!hasMoreReplies.value || isLoadingReplies.value || isLoadingMoreReplies.value) {
        return;
    }

    isLoadingMoreReplies.value = true;

    try {
        const nextPage = replyPage.value + 1;
        const response = await forumApi.getComments({
            isTopic: false,
            recordId: localComment.value.id,
            page: nextPage,
        });

        const values = normalizeItems(response);
        replies.value = [...replies.value, ...values];
        replyPage.value = nextPage;
        hasMoreReplies.value = values.length === 5;
    } catch (error) {
        errorMessage.value = error?.message || 'Failed to load more replies.';
    } finally {
        isLoadingMoreReplies.value = false;
    }
}

async function toggleReplies() {
    if (!hasReplies.value) {
        return;
    }

    isRepliesOpen.value = !isRepliesOpen.value;
    if (isRepliesOpen.value && !replies.value.length) {
        await loadReplies({ reset: true });
    }
}

function toggleReplyForm() {
    isReplyFormOpen.value = !isReplyFormOpen.value;
    if (!isReplyFormOpen.value) {
        replyText.value = '';
    }
}

async function submitReply() {
    if (!replyText.value.trim() || isCreatingReply.value) {
        return;
    }

    clearMessages();
    isCreatingReply.value = true;

    try {
        const response = await forumApi.createComment({
            postId: localComment.value.id,
            isTopic: false,
            universityName: props.universityName,
            commentContent: replyText.value.trim(),
        });

        actionMessage.value = readMessage(response) || 'Reply created.';
        replyText.value = '';
        isReplyFormOpen.value = false;
        localComment.value.repliesCount += 1;

        if (isRepliesOpen.value) {
            await loadReplies({ reset: true });
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not create reply.';
    } finally {
        isCreatingReply.value = false;
    }
}

async function likeComment() {
    if (isLiking.value) {
        return;
    }

    clearMessages();
    isLiking.value = true;

    try {
        const response = await forumApi.likeComment(localComment.value.id);
        const message = readMessage(response);
        actionMessage.value = message || 'Comment reaction updated.';

        if (message.toLowerCase().includes('removed')) {
            localComment.value.likesOverall = Math.max(0, localComment.value.likesOverall - 1);
        } else {
            localComment.value.likesOverall += 1;
            if (message.toLowerCase().includes('liked') && localComment.value.dislikesOverall > 0) {
                localComment.value.dislikesOverall -= 1;
            }
        }
    } catch (error) {
        errorMessage.value = error?.message || 'Could not update like.';
    } finally {
        isLiking.value = false;
    }
}

async function dislikeComment() {
    if (isDisliking.value) {
        return;
    }

    clearMessages();
    isDisliking.value = true;

    try {
        const response = await forumApi.dislikeComment(localComment.value.id);
        const message = readMessage(response);
        actionMessage.value = message || 'Comment reaction updated.';

        if (message.toLowerCase().includes('removed')) {
            localComment.value.dislikesOverall = Math.max(0, localComment.value.dislikesOverall - 1);
        } else {
            localComment.value.dislikesOverall += 1;
            if (message.toLowerCase().includes('disliked') && localComment.value.likesOverall > 0) {
                localComment.value.likesOverall -= 1;
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
    <article class="comment-node" :style="paddingStyle">
        <div class="comment-node__card">
            <header class="comment-node__header">
                <div class="comment-node__author">
                    <AppIcon name="profile" :boxed="false" />
                    <p>{{ localComment.creatorName }}</p>
                    <span :class="roleBadgeClass">{{ localComment.creatorRole }}</span>
                </div>

                <p class="comment-node__time">{{ createdAtLabel }}</p>
            </header>

            <p class="comment-node__content">{{ localComment.commentContent }}</p>

            <footer class="comment-node__actions">
                <button class="action-chip" type="button" title="Like" aria-label="Like comment" @click="likeComment" :disabled="isLiking || isDisliking">
                    <AppIcon name="vote-up" :boxed="false" />
                    <span class="action-chip__count">{{ localComment.likesOverall }}</span>
                </button>
                <button class="action-chip" type="button" title="Dislike" aria-label="Dislike comment" @click="dislikeComment" :disabled="isLiking || isDisliking">
                    <AppIcon name="vote-down" :boxed="false" />
                    <span class="action-chip__count">{{ localComment.dislikesOverall }}</span>
                </button>
                <button class="action-chip" type="button" title="Reply" aria-label="Write reply" @click="toggleReplyForm" :disabled="isCreatingReply">
                    <AppIcon :name="isReplyFormOpen ? 'chevron-down' : 'reply'" :boxed="false" />
                </button>
                <button
                    v-if="hasReplies"
                    class="action-chip"
                    type="button"
                    :title="isRepliesOpen ? 'Hide child replies' : 'Show child replies'"
                    :aria-label="isRepliesOpen ? 'Hide child replies' : 'Show child replies'"
                    @click="toggleReplies"
                    :disabled="isLoadingReplies"
                >
                    <AppIcon :name="isRepliesOpen ? 'chevron-down' : 'comment'" :boxed="false" />
                    <span class="action-chip__count">{{ localComment.repliesCount }}</span>
                </button>
            </footer>

            <p v-if="errorMessage" class="state-message state-message--error">{{ errorMessage }}</p>
            <p v-else-if="actionMessage" class="state-message state-message--success">{{ actionMessage }}</p>

            <form v-if="isReplyFormOpen" class="comment-node__reply-form" @submit.prevent="submitReply">
                <textarea
                    v-model="replyText"
                    class="input-field comment-node__textarea"
                    rows="3"
                    maxlength="1000"
                    placeholder="Write your reply"
                />
                <button class="action-chip action-chip--send" type="submit" title="Send" aria-label="Send reply" :disabled="isCreatingReply || !replyText.trim()">
                    <AppIcon name="send" :boxed="false" />
                </button>
            </form>
        </div>

        <section v-if="isRepliesOpen" class="comment-node__replies">
            <p v-if="isLoadingReplies" class="state-message">Loading replies...</p>
            <p v-else-if="!replies.length" class="state-message">No replies found on this page.</p>

            <ForumCommentNode
                v-for="reply in replies"
                :key="reply.id || reply.Id"
                :comment="reply"
                :university-name="universityName"
                :depth="depth + 1"
            />

            <footer v-if="hasMoreReplies" class="comment-node__more-row">
                <button class="action-chip" type="button" title="Load more replies" aria-label="Load more replies" @click="loadMoreReplies" :disabled="isLoadingMoreReplies">
                    <AppIcon name="comment" :boxed="false" />
                    <span>{{ isLoadingMoreReplies ? 'Loading' : 'More' }}</span>
                </button>
            </footer>
        </section>
    </article>
</template>

<style scoped>
.comment-node {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.comment-node__card {
    padding: 0.35rem 0;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    border-left: 2px solid rgba(143, 44, 226, 0.18);
    padding-left: 0.55rem;
}

.comment-node__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.8rem;
}

.comment-node__author {
    display: inline-flex;
    align-items: center;
    gap: 0.42rem;
}

.comment-node__author p {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 600;
    font-size: 0.9rem;
}

.comment-node__time {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.8rem;
}

.comment-node__content {
    margin: 0;
    color: var(--ttl-text-primary);
    line-height: 1.45;
    white-space: pre-wrap;
    word-break: break-word;
    font-size: 0.92rem;
}

.comment-node__actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.42rem;
}

.action-chip {
    min-height: 1.9rem;
    border: 1px solid rgba(143, 44, 226, 0.16);
    border-radius: 999px;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.92), rgba(143, 44, 226, 0.05));
    color: var(--ttl-accent-dark);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.3rem;
    padding: 0 0.54rem;
    cursor: pointer;
    transition: transform var(--ttl-transition-fast), border-color var(--ttl-transition-base), background var(--ttl-transition-base);
}

.action-chip:hover {
    transform: translateY(-1px);
    border-color: rgba(143, 44, 226, 0.3);
}

.action-chip:disabled {
    opacity: 0.6;
    cursor: default;
}

.action-chip--send {
    align-self: flex-end;
}

.action-chip__count {
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
    line-height: 1;
}

.comment-node__reply-form {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.comment-node__textarea {
    resize: vertical;
}

.comment-node__replies {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.comment-node__more-row {
    display: flex;
    justify-content: flex-start;
}

.state-message {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
}

.state-message--error {
    color: var(--ttl-danger);
}

.state-message--success {
    color: var(--ttl-success);
}

@media (max-width: 700px) {
    .comment-node__header {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>

