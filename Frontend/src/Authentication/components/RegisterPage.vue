<script setup>

import { ref } from 'vue';
import { useRouter } from 'vue-router';

import authApi from '../services/api';

const username = ref('');
const email = ref('');
const password = ref('');
const errorMessage = ref('');

const router = useRouter();

async function handleRegister(){
    try {
        let result = await authApi.register(this.email, this.password);
        if(result.success)
            router.push({ name: 'Dashboard'})
        else
            this.errorMessage = result.message;
      } catch (error) {
        this.errorMessage = error.message;
      }
}
</script>

<template>
    <div>
        <h1>Register</h1>
        <form @submit.prevent="handleRegister">
            <input v-model="username" type="text" placeholder="Username" required />
            <input v-model="email" type="email" placeholder="Email" required />
            <input v-model="password" type="password" placeholder="Password" required />
            <button type="submit">Login</button>
        </form>
        <p>{{ errorMessage }}</p>
    </div>
</template>
