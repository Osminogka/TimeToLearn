<script setup>
import { computed, ref } from 'vue';
import markdownService from '@/Courses/services/markdownService';

const props = defineProps({
    modelValue: {
        type: String,
        default: '',
    },
    disabled: {
        type: Boolean,
        default: false,
    },
    maxlength: {
        type: Number,
        default: 10000,
    },
    placeholder: {
        type: String,
        default: 'Write lesson content in markdown...',
    },
});

const emit = defineEmits(['update:modelValue']);
const textareaRef = ref(null);
const activeTab = ref('write');

const previewHtml = computed(() => markdownService.renderMarkdownToSafeHtml(props.modelValue));

function updateValue(value) {
    emit('update:modelValue', value);
}

function setTab(tab) {
    activeTab.value = tab;
}

function applyWrap(prefix, suffix = '', placeholder = '') {
    const el = textareaRef.value;
    if (!el) {
        updateValue(`${props.modelValue}${prefix}${placeholder}${suffix}`);
        return;
    }

    const start = el.selectionStart;
    const end = el.selectionEnd;
    const source = props.modelValue || '';
    const selected = source.slice(start, end) || placeholder;

    const nextValue = `${source.slice(0, start)}${prefix}${selected}${suffix}${source.slice(end)}`;
    updateValue(nextValue);

    requestAnimationFrame(() => {
        el.focus();
        const nextPosition = start + prefix.length + selected.length + suffix.length;
        el.setSelectionRange(nextPosition, nextPosition);
    });
}
</script>

<template>
    <div class="markdown-editor">
        <div class="markdown-editor__tabs">
            <button
                class="secondary-button markdown-editor__tab"
                type="button"
                :class="{ 'markdown-editor__tab--active': activeTab === 'write' }"
                :disabled="disabled"
                @click="setTab('write')"
            >
                Write
            </button>
            <button
                class="secondary-button markdown-editor__tab"
                type="button"
                :class="{ 'markdown-editor__tab--active': activeTab === 'preview' }"
                :disabled="disabled"
                @click="setTab('preview')"
            >
                Preview
            </button>
        </div>

        <div v-if="activeTab === 'write'" class="markdown-editor__write">
            <div class="markdown-editor__toolbar">
                <button class="secondary-button markdown-editor__tool" type="button" :disabled="disabled" @click="applyWrap('**', '**', 'bold')">B</button>
                <button class="secondary-button markdown-editor__tool" type="button" :disabled="disabled" @click="applyWrap('_', '_', 'italic')">I</button>
                <button class="secondary-button markdown-editor__tool" type="button" :disabled="disabled" @click="applyWrap('## ', '', 'Heading')">H2</button>
                <button class="secondary-button markdown-editor__tool" type="button" :disabled="disabled" @click="applyWrap('- ', '', 'List item')">List</button>
                <button class="secondary-button markdown-editor__tool" type="button" :disabled="disabled" @click="applyWrap('`', '`', 'code')">Code</button>
                <button
                    class="secondary-button markdown-editor__tool"
                    type="button"
                    :disabled="disabled"
                    @click="applyWrap('[', '](https://example.com)', 'Link title')"
                >
                    Link
                </button>
            </div>

            <textarea
                ref="textareaRef"
                class="input-field markdown-editor__textarea"
                :disabled="disabled"
                :maxlength="maxlength"
                :placeholder="placeholder"
                :value="modelValue"
                @input="updateValue($event.target.value)"
            />
        </div>

        <article v-else class="surface-card markdown-editor__preview section-copy" v-html="previewHtml" />
    </div>
</template>

<style scoped>
.markdown-editor {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.markdown-editor__tabs {
    display: flex;
    gap: 0.5rem;
}

.markdown-editor__tab {
    min-height: 2.2rem;
    min-width: 6.5rem;
}

.markdown-editor__tab--active {
    border-color: rgba(143, 44, 226, 0.32);
    background: rgba(143, 44, 226, 0.12);
}

.markdown-editor__write {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.markdown-editor__toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
}

.markdown-editor__tool {
    min-height: 2rem;
    min-width: 3.1rem;
    font-size: 0.8rem;
    padding: 0.4rem 0.55rem;
}

.markdown-editor__textarea {
    min-height: 11rem;
    resize: vertical;
}

.markdown-editor__preview {
    padding: 0.9rem;
    min-height: 11rem;
    line-height: 1.65;
}

.markdown-editor__preview :deep(h1),
.markdown-editor__preview :deep(h2),
.markdown-editor__preview :deep(h3) {
    color: var(--ttl-text-primary);
    margin: 0.3rem 0 0.5rem;
}

.markdown-editor__preview :deep(p),
.markdown-editor__preview :deep(ul),
.markdown-editor__preview :deep(ol) {
    margin: 0.45rem 0;
}

.markdown-editor__preview :deep(code) {
    background: rgba(143, 44, 226, 0.12);
    border-radius: 0.35rem;
    padding: 0.05rem 0.32rem;
}
</style>
