<script setup>
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import InfoCardGrid from '../components/InfoCardGrid.vue';

const router = useRouter();
const route = useRoute();

const isAuthFormVisible = computed(() => {
    return ['Login', 'Register'].includes(route.name);
});
</script>

<template>
    <div class="page-container">
        <header>
            <h1 class="gradient-title" @click="router.push('/')">Time to Learn!</h1>
            <div class="register-buttons">
                <router-link v-if="route.name !== 'Login'" :to="{ name: 'Login' }"
                    class="redirect-button">Login</router-link>

                <router-link v-if="route.name !== 'Register'" :to="{ name: 'Register' }"
                    class="redirect-button">Register</router-link>
            </div>
        </header>

        <main>
            <div class="content-wrapper">
                <Transition name="slide-fade" mode="out-in">
                    <div class="form-section" v-if="isAuthFormVisible" key="auth">
                        <router-view v-slot="{ Component }">
                            <component :is="Component" />
                        </router-view>
                    </div>

                    <div class="info-wrapper" v-else key="info">
                        <InfoCardGrid />
                    </div>
                </Transition>
            </div>
        </main>
    </div>
</template>

<style scoped>
.page-container {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background-color: var(--ttl-bg-page);
}

header {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    align-items: center;
    background-color: var(--ttl-bg-surface);
    border-bottom: 1px solid var(--ttl-border-subtle);
}

.gradient-title {
    font-size: 1.8rem;
    cursor: pointer;
}

.register-buttons {
    display: flex;
    gap: 10px;
}

.redirect-button {
    padding: 0.6rem 1.2rem;
    text-decoration: none;
    border-radius: 5px;
    background-color: var(--ttl-accent);
    color: white;
    font-weight: 600;
}

main {
    flex: 1;
    display: flex;
    justify-content: center;
    padding: 1.5rem 1rem;
}

.content-wrapper {
    width: 100%;
    max-width: 1000px;
}

.form-section {
    max-width: 450px;
    margin: 0 auto;
}

@media (min-width: 768px) {
    header {
        flex-direction: row;
        justify-content: space-between;
        padding: 1rem 5%;
    }
    .gradient-title { font-size: 2.5rem; }
    main { padding: 3rem 2rem; }
}
</style>