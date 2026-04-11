<script setup>
defineProps({
    name: {
        type: String,
        required: true,
    },
});
</script>

<template>
    <span class="app-icon" :class="`app-icon--${name}`" aria-hidden="true">
        <svg v-if="name === 'dashboard'" viewBox="0 0 24 24" fill="none">
            <path d="M4 11.5V6.8C4 6.36 4.36 6 4.8 6h6.4c0.44 0 .8.36.8.8v4.7c0 .44-.36.8-.8.8H4.8c-.44 0-.8-.36-.8-.8ZM12 17.2v-4.7c0-.44.36-.8.8-.8h6.4c.44 0 .8.36.8.8v4.7c0 .44-.36.8-.8.8h-6.4c-.44 0-.8-.36-.8-.8ZM4 17.2V15.4c0-.44.36-.8.8-.8h2.4c.44 0 .8.36.8.8v1.8c0 .44-.36.8-.8.8H4.8c-.44 0-.8-.36-.8-.8ZM14.4 9.4h4.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
        </svg>

        <svg v-else-if="name === 'users'" viewBox="0 0 24 24" fill="none">
            <path d="M15.5 11.2a3.2 3.2 0 1 0-3.2-3.2 3.2 3.2 0 0 0 3.2 3.2ZM8.2 12.2a2.7 2.7 0 1 0-2.7-2.7 2.7 2.7 0 0 0 2.7 2.7ZM3.8 18.2c0-2.2 2.2-4 4.9-4s4.9 1.8 4.9 4M13.8 18.2c.4-2.4 2.4-4.1 5-4.1 2.2 0 4 1.1 4.4 2.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        <svg v-else-if="name === 'create'" viewBox="0 0 24 24" fill="none">
            <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            <path d="M7.5 7.5h9v9h-9z" stroke="currentColor" stroke-width="1.5" opacity="0.55"/>
        </svg>

        <svg v-else-if="name === 'account'" viewBox="0 0 24 24" fill="none">
            <path d="M12 12.2a4.1 4.1 0 1 0-4.1-4.1A4.1 4.1 0 0 0 12 12.2ZM4.7 19.3c.8-3.3 4.1-5.2 7.3-5.2s6.5 1.9 7.3 5.2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        <svg v-else-if="name === 'logout'" viewBox="0 0 24 24" fill="none">
            <path d="M10.5 5.2H5.8A1.8 1.8 0 0 0 4 7v10a1.8 1.8 0 0 0 1.8 1.8h4.7M14.8 16.2 19 12l-4.2-4.2M19 12H9.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        <svg v-else-if="name === 'profile'" viewBox="0 0 24 24" fill="none">
            <path d="M12 13.1a4 4 0 1 0-4-4 4 4 0 0 0 4 4ZM5 19.2c.9-2.8 3.5-4.6 7-4.6s6.1 1.8 7 4.6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        <svg v-else-if="name === 'role'" viewBox="0 0 24 24" fill="none">
            <path d="M12 3.8 19 7.5v5c0 4.1-3 7.7-7 8.9-4-1.2-7-4.8-7-8.9v-5l7-3.7Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/>
            <path d="M9.7 12.2 11.4 14l3.1-3.4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        <svg v-else-if="name === 'university'" viewBox="0 0 24 24" fill="none">
            <path d="M12 4.2 3.8 8.1 12 12l8.2-3.9-8.2-3.9ZM6.5 11.2V15c0 1.8 2.5 3.3 5.5 3.3s5.5-1.5 5.5-3.3v-3.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M19 8.8v5.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
        </svg>

        <svg v-else-if="name === 'join'" viewBox="0 0 24 24" fill="none">
            <path d="M7.5 12h9M12 7.5v9" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            <path d="M5.2 5.2h13.6v13.6H5.2z" stroke="currentColor" stroke-width="1.5" opacity="0.55"/>
        </svg>
    </span>
</template>

<style scoped>
.app-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.5rem;
    height: 2.5rem;
    border-radius: 0.9rem;
    background: linear-gradient(135deg, rgba(143, 44, 226, 0.12), rgba(255, 95, 162, 0.14));
    color: var(--ttl-accent-dark);
    flex: 0 0 auto;
}

.app-icon svg {
    width: 1.25rem;
    height: 1.25rem;
}
</style>