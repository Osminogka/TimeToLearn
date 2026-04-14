const RESOURCE_PREFIX = '<!-- TTL_RESOURCES ';
const RESOURCE_SUFFIX = ' -->';

const ALLOWED_TYPES = ['video', 'material', 'article', 'slides', 'repository', 'other'];

function toStringSafe(value) {
    return String(value || '').trim();
}

function normalizeType(type) {
    const normalized = toStringSafe(type).toLowerCase();
    return ALLOWED_TYPES.includes(normalized) ? normalized : 'other';
}

function normalizeResource(resource) {
    const title = toStringSafe(resource?.title);
    const url = toStringSafe(resource?.url);
    const type = normalizeType(resource?.type);

    if (!url) {
        return null;
    }

    return {
        title: title || url,
        url,
        type,
    };
}

function normalizeResources(resources) {
    if (!Array.isArray(resources)) {
        return [];
    }

    const uniqueByUrl = new Map();

    resources
        .map(normalizeResource)
        .filter(Boolean)
        .forEach((resource) => {
            if (!uniqueByUrl.has(resource.url)) {
                uniqueByUrl.set(resource.url, resource);
            }
        });

    return Array.from(uniqueByUrl.values());
}

function createLegacyResource(url, type, title) {
    const normalizedUrl = toStringSafe(url);
    if (!normalizedUrl) {
        return null;
    }

    return normalizeResource({
        url: normalizedUrl,
        type,
        title,
    });
}

function getEmbeddedPayload(content) {
    const source = String(content || '');
    const start = source.lastIndexOf(RESOURCE_PREFIX);
    if (start === -1) {
        return null;
    }

    const end = source.indexOf(RESOURCE_SUFFIX, start);
    if (end === -1) {
        return null;
    }

    const payload = source.slice(start + RESOURCE_PREFIX.length, end);
    return payload.trim();
}

function extractEmbeddedResources(content) {
    const payload = getEmbeddedPayload(content);
    if (!payload) {
        return [];
    }

    try {
        const parsed = JSON.parse(payload);
        return normalizeResources(parsed?.items || []);
    } catch {
        return [];
    }
}

function removeEmbeddedResourcesFromContent(content) {
    const source = String(content || '');
    const start = source.lastIndexOf(RESOURCE_PREFIX);
    if (start === -1) {
        return source;
    }

    const end = source.indexOf(RESOURCE_SUFFIX, start);
    if (end === -1) {
        return source;
    }

    return `${source.slice(0, start)}${source.slice(end + RESOURCE_SUFFIX.length)}`.trim();
}

function removeResourceCommentFromHtml(html) {
    const source = String(html || '');
    return source.replace(/<!--\s*TTL_RESOURCES[\s\S]*?-->/g, '').trim();
}

function embedResourcesInContent(content, resources) {
    const baseContent = removeEmbeddedResourcesFromContent(content);
    const normalized = normalizeResources(resources);

    if (!normalized.length) {
        return baseContent;
    }

    const payload = JSON.stringify({ version: 1, items: normalized });
    return `${baseContent}\n\n${RESOURCE_PREFIX}${payload}${RESOURCE_SUFFIX}`;
}

function getLessonResources(lesson) {
    if (!lesson) {
        return [];
    }

    const content = lesson.content || lesson.Content || '';
    const resources = extractEmbeddedResources(content);

    const legacyVideo = createLegacyResource(
        lesson.videoLink || lesson.VideoLink,
        'video',
        'Video material'
    );

    const legacyMaterial = createLegacyResource(
        lesson.materialLink || lesson.MaterialLink,
        'material',
        'Additional material'
    );

    return normalizeResources([
        ...resources,
        legacyVideo,
        legacyMaterial,
    ]);
}

function buildApiResourceFields(resources) {
    const normalized = normalizeResources(resources);
    const firstVideo = normalized.find((item) => item.type === 'video');
    const firstNonVideo = normalized.find((item) => item.type !== 'video');

    return {
        videoLink: firstVideo?.url || null,
        materialLink: firstNonVideo?.url || null,
        resources: normalized,
    };
}

export default {
    normalizeResources,
    extractEmbeddedResources,
    removeEmbeddedResourcesFromContent,
    removeResourceCommentFromHtml,
    embedResourcesInContent,
    getLessonResources,
    buildApiResourceFields,
};
