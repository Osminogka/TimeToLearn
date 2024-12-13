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

function verifyRegister(){
    if(email.value === '' || password.value === '') {
        this.errorMessage = 'Please fill in all fields';
    }
    if(password.value.length < 8) {
        this.errorMessage = 'Password must be at least 8 characters';
    }
    if(!email.value.includes('@') || !email.value.includes('.')) {
        this.errorMessage = 'Invalid email address';
    }
    if(this.errorMessage === '') {
        handleLogin();
    }
}
</script>

<template>
    <div class="container">
        <h1 class="default-text">Register</h1>
        <form class="login-form" @submit.prevent="verifyRegister">
            <input class="input-field" v-model="username" type="text" placeholder="Username" required />
            <input class="input-field" v-model="email" type="email" placeholder="Email" required />
            <input class="input-field" v-model="password" type="password" placeholder="Password" required />
            <button class="submit-button" type="submit">Login</button>
        </form>
        <p class="default-text">{{ errorMessage }}</p>
    </div>
</template>

<style scoped>
@import '@/assets/css/text-classes.css';
@import '@/assets/css/login-register.css';

</style>
