<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';

import authApi from '../services/api';

const email = ref('');
const password = ref('');
const errorMessage = ref('');

const router = useRouter();

async function handleLogin() {
    try {
        let result = await authApi.login(email.value, password.value);
        if (result.success) {
            router.push('/');
        } else {
            errorMessage.value = result.message;
        }
    } catch (error) {
        errorMessage.value = error.message;
    }
}

function verifyLogin() {
    // Reset the error message before validation starts
    errorMessage.value = '';

    if (email.value === '' || password.value === '') {
        errorMessage.value = 'Please fill in all fields';
    } else if (password.value.length < 8) {
        errorMessage.value = 'Password must be at least 8 characters';
    } else if (!email.value.includes('@') || !email.value.includes('.')) {
        errorMessage.value = 'Invalid email address';
    }

    // If no error message, proceed to login
    if (errorMessage.value === '') {
        handleLogin();
    }
}
</script>

<template>
    <div class="container">
        <h1 class="default-text">Login</h1>
        <form class="login-form">
            <input class="input-field" v-model="email" type="email" placeholder="Email" required />
            <input class="input-field" v-model="password" type="password" placeholder="Password" required />
            <button class="submit-button" @click.prevent="verifyLogin()">Login</button>
        </form>
        <p class="default-text">{{ errorMessage }}</p>
    </div>
</template>

<style scoped>
@import '@/Authentication/assets/css/text-classes.css';
@import '@/Authentication/assets/css/login-register.css';
</style>
