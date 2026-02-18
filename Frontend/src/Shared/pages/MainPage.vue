<script setup>
import Dashboard from '../Core/universities/pages/DashboardUniversties.vue';
import GreetingPage from './GreetingPage.vue';
import utils from '../services/utils';
</script>

<template>
    <GreetingPage v-if="!utils.isAuthenticated()"/>
    <Dashboard v-else />
</template>