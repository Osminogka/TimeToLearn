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

async function toggleReplies() {
    if (!hasReplies.value) {
        return;
    }

    isRepliesOpen.value = !isRepliesOpen.value;
    if (isRepliesOpen.value && !replies.value.length) {
        await loadReplies({ reset: true });
    }
}

async function goToPrevReplyPage() {
    if (replyPage.value <= 0 || isLoadingReplies.value) {
        return;
    }

    replyPage.value -= 1;
    await loadReplies();
}

async function goToNextReplyPage() {
    if (!hasMoreReplies.value || isLoadingReplies.value) {
        return;
    }

    replyPage.value += 1;
    await loadReplies();
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
        <div class="comment-node__card surface-card">
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
                <button class="secondary-button" type="button" @click="likeComment" :disabled="isLiking || isDisliking">
                    Like {{ localComment.likesOverall }}
                </button>
                <button class="secondary-button" type="button" @click="dislikeComment" :disabled="isLiking || isDisliking">
                    Dislike {{ localComment.dislikesOverall }}
                </button>
                <button class="secondary-button" type="button" @click="toggleReplyForm" :disabled="isCreatingReply">
                    {{ isReplyFormOpen ? 'Cancel reply' : 'Reply' }}
                </button>
                <button
                    v-if="hasReplies"
                    class="secondary-button"
                    type="button"
                    @click="toggleReplies"
                    :disabled="isLoadingReplies"
                >
                    {{ isRepliesOpen ? 'Collapse replies' : `Expand replies (${localComment.repliesCount})` }}
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
                <button class="submit-button" type="submit" :disabled="isCreatingReply || !replyText.trim()">
                    {{ isCreatingReply ? 'Posting...' : 'Post reply' }}
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

            <footer class="comment-node__pagination">
                <button class="secondary-button" type="button" @click="goToPrevReplyPage" :disabled="replyPage <= 0 || isLoadingReplies">
                    Previous replies
                </button>
                <p class="pagination-status">Page {{ replyPage + 1 }}</p>
                <button class="secondary-button" type="button" @click="goToNextReplyPage" :disabled="!hasMoreReplies || isLoadingReplies">
                    Next replies
                </button>
            </footer>
        </section>
    </article>
</template>

<style scoped>
.comment-node {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.comment-node__card {
    padding: 0.9rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
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
    gap: 0.5rem;
}

.comment-node__author p {
    margin: 0;
    color: var(--ttl-text-primary);
    font-weight: 700;
}

.comment-node__time {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
}

.comment-node__content {
    margin: 0;
    color: var(--ttl-text-primary);
    line-height: 1.55;
    white-space: pre-wrap;
    word-break: break-word;
}

.comment-node__actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
}

.comment-node__reply-form {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.comment-node__textarea {
    resize: vertical;
}

.comment-node__replies {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
}

.comment-node__pagination {
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
    .comment-node__header {
        flex-direction: column;
        align-items: flex-start;
    }

    .comment-node__pagination {
        flex-direction: column;
        align-items: stretch;
    }

    .comment-node__pagination .secondary-button {
        width: 100%;
    }
}
</style>
