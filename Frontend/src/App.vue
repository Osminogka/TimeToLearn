<script setup>

import { isAuthenticated, getCurrentUser } from './Authentication/services/utils';

import { useRouter } from 'vue-router';

getCurrentUser();

const router = useRouter();
router.push({ name: isAuthenticated() ? 'Dashboard' : 'GreetingPage' });

</script>

<template>
  <RouterView />
</template>