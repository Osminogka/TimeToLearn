<script setup>
import authUtils from '@/Shared/services/utils';

import { useRouter } from 'vue-router';

const router = useRouter();

function logout(){
    authUtils.clearToken();
    window.location.reload();
}
</script>

<template>
    <h1 class="default-text">Dashboard</h1>
    <button @click="logout" class="submit-button">Logout</button>
</template>

<style scoped>
@import '@/assets/css/text-classes.css';
</style>