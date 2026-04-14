import { marked } from 'marked';
import DOMPurify from 'dompurify';

const SAFE_URL_PATTERN = /^(https?:|mailto:|tel:|\/|#)/i;
let hooksInitialized = false;

marked.setOptions({
    gfm: true,
    breaks: true,
});

function ensureHooks() {
    if (hooksInitialized) {
        return;
    }

    DOMPurify.addHook('uponSanitizeAttribute', (_node, data) => {
        const attrName = String(data.attrName || '').toLowerCase();
        const attrValue = String(data.attrValue || '').trim();

        if (attrName.startsWith('on')) {
            data.keepAttr = false;
            return;
        }

        if ((attrName === 'href' || attrName === 'src' || attrName === 'xlink:href') && attrValue && !SAFE_URL_PATTERN.test(attrValue)) {
            data.keepAttr = false;
        }
    });

    hooksInitialized = true;
}

function sanitizeHtml(unsafeHtml) {
    ensureHooks();

    return DOMPurify.sanitize(String(unsafeHtml || ''), {
        USE_PROFILES: { html: true },
        FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed', 'form'],
        FORBID_ATTR: ['style', 'srcset'],
    });
}

function renderMarkdownToSafeHtml(markdownText) {
    const rendered = marked.parse(String(markdownText || ''));
    return sanitizeHtml(rendered);
}

export default {
    sanitizeHtml,
    renderMarkdownToSafeHtml,
};
