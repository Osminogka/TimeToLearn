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
    border: 1px solid #ddd;
    border-radius: 4px;
    overflow: hidden;
    background: #fff;
}

.markdown-editor__tabs {
    display: flex;
    border-bottom: 1px solid #ddd;
    background: #f6f8fa;
}

.markdown-editor__tab {
    flex: 1;
    min-height: 2.4rem;
    border: none;
    background: #f6f8fa;
    border-bottom: 2px solid transparent;
    color: #666;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s ease;
}

.markdown-editor__tab:hover {
    color: #333;
    background: #fff;
}

.markdown-editor__tab--active {
    color: #0366d6;
    border-bottom-color: #0366d6;
    background: #fff;
}

.markdown-editor__tab:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.markdown-editor__write {
    display: flex;
    flex-direction: column;
}

.markdown-editor__toolbar {
    display: flex;
    gap: 2px;
    padding: 8px;
    border-bottom: 1px solid #ddd;
    background: #f6f8fa;
    flex-wrap: wrap;
}

.markdown-editor__tool {
    height: 28px;
    padding: 4px 8px;
    background: #fff;
    border: 1px solid #ddd;
    border-radius: 3px;
    color: #333;
    font-size: 12px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s ease;
}

.markdown-editor__tool:hover:not(:disabled) {
    background: #f3f3f3;
    border-color: #999;
    color: #000;
}

.markdown-editor__tool:active:not(:disabled) {
    background: #e0e0e0;
}

.markdown-editor__tool:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.markdown-editor__textarea {
    flex: 1;
    min-height: 300px;
    padding: 10px;
    border: none;
    font-family: 'Courier New', Courier, monospace;
    font-size: 13px;
    line-height: 1.5;
    resize: vertical;
    background: #fff;
    color: #333;
}

.markdown-editor__textarea:focus {
    outline: none;
    background: #fafbfc;
}

.markdown-editor__preview {
    padding: 15px;
    min-height: 300px;
    line-height: 1.7;
    background: #fff;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', Arial, sans-serif;
    color: #333;
}

.markdown-editor__preview :deep(h1),
.markdown-editor__preview :deep(h2),
.markdown-editor__preview :deep(h3),
.markdown-editor__preview :deep(h4),
.markdown-editor__preview :deep(h5),
.markdown-editor__preview :deep(h6) {
    color: #000;
    margin: 16px 0 8px 0;
    font-weight: 600;
}

.markdown-editor__preview :deep(h1) {
    font-size: 28px;
    border-bottom: 1px solid #eee;
    padding-bottom: 8px;
}

.markdown-editor__preview :deep(h2) {
    font-size: 24px;
}

.markdown-editor__preview :deep(h3) {
    font-size: 20px;
}

.markdown-editor__preview :deep(p) {
    margin: 8px 0;
}

.markdown-editor__preview :deep(ul),
.markdown-editor__preview :deep(ol) {
    margin: 8px 0;
    padding-left: 20px;
}

.markdown-editor__preview :deep(li) {
    margin: 4px 0;
}

.markdown-editor__preview :deep(code) {
    background: #f3f3f3;
    border: 1px solid #ddd;
    border-radius: 3px;
    padding: 2px 6px;
    font-family: 'Courier New', Courier, monospace;
    font-size: 12px;
    color: #c7254e;
}

.markdown-editor__preview :deep(pre) {
    background: #f6f8fa;
    border: 1px solid #ddd;
    border-radius: 3px;
    padding: 12px;
    overflow: auto;
    margin: 8px 0;
}

.markdown-editor__preview :deep(pre code) {
    background: none;
    border: none;
    padding: 0;
    color: #333;
}

.markdown-editor__preview :deep(blockquote) {
    border-left: 4px solid #ddd;
    padding-left: 12px;
    margin: 8px 0;
    color: #666;
}

.markdown-editor__preview :deep(a) {
    color: #0366d6;
    text-decoration: none;
}

.markdown-editor__preview :deep(a:hover) {
    text-decoration: underline;
}
</style>
