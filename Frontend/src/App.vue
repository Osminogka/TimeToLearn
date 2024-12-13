<script setup>

import { isAuthenticated, getCurrentUser } from './Authentication/services/utils';
import GreetingPage from './components/GreetingPage.vue';
import Dashboard from './components/Dashboard.vue';

import { useRouter } from 'vue-router';

const router = useRouter();

</script>

<template>
  <div v-if="!isAuthenticated()">
    <GreetingPage />
  </div>
  <div v-else>
    <Dashboard />
  </div>
</template>