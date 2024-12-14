<script setup>

import { isAuthenticated, getCurrentUser, user } from './Authentication/services/utils';
import GreetingPage from './components/GreetingPage.vue';
import Dashboard from './components/Dashboard.vue';
import DefineRole from './Authentication/components/DefineRole.vue';
import authApi from './Authentication/services/api';

import { useRouter } from 'vue-router';
import { reactive, ref, onBeforeMount } from 'vue';

const router = useRouter();

const userRole = reactive({
  isTeacher: false,
  isStudent: false,
});
const errorMessage = ref('');

async function getUserRole(){
  let result = await authApi.getRole(user.value.email);
  if (result.success) {
    userRole.isTeacher = result.value.isTeacher;
    userRole.isStudent = result.value.isStudent;
  } else {
    errorMessage.value = result.message;
  }
}

onBeforeMount(async () =>{
  getCurrentUser();
  if(isAuthenticated())
    await getUserRole();
});

</script>

<template>
  <div v-if="!isAuthenticated()">
    <GreetingPage />
  </div>
  <div v-else-if="isAuthenticated() && !userRole.isTeacher && !userRole.isStudent">
    <DefineRole />
  </div>
  <div v-else-if="isAuthenticated() && (userRole.isTeacher || userRole.isStudent)">
    <Dashboard />
  </div>
  <p>{{ errorMessage }}</p>
</template>