<script setup>

import { ref } from 'vue';
import { useRouter } from 'vue-router';

import authApi from '../services/api';

const email = ref('');
const password = ref('');
const errorMessage = ref('');

const router = useRouter();

async function handleLogin(){
    try {
        let result = await authApi.login(this.email, this.password);
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
        <h1>Login</h1>
        <form>
            <input v-model="email" type="email" placeholder="Email" required />
            <input v-model="password" type="password" placeholder="Password" required />
            <button type="submit" @click.prevent="handleLogin()">Login</button>
        </form>
        <p>{{ errorMessage }}</p>
    </div>
</template>
