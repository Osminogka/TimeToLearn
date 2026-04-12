<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import PrimaryNavigation from '@/Shared/components/PrimaryNavigation.vue';
import { isAuthenticated } from '@/Shared/services/utils';

const route = useRoute();

const showPrimaryNavigation = computed(() => {
    if (!isAuthenticated()) {
        return false;
    }

    return Boolean(route.meta.requiresAuth) && !route.meta.hidePrimaryNavigation;
});
</script>

<template>
    <main class="app-shell" :class="{ 'app-shell--mobile-nav': showPrimaryNavigation }">
        <PrimaryNavigation v-if="showPrimaryNavigation" />
        <RouterView />
    </main>
</template>

<style scoped>
.app-shell {
    min-height: 100vh;
}

@media (max-width: 760px) {
    .app-shell--mobile-nav {
        padding-bottom: 5.4rem;
    }
}
</style>