<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';

import authApi from '../services/authApi';

const username = ref('');
const email = ref('');
const password = ref('');
const errorMessage = ref('');

const router = useRouter();

async function handleRegister(){
    try {
        let result = await authApi.register(username.value, email.value, password.value);
        if(result.success)
            router.push({ name: 'Main' })
        else
            errorMessage.value = result.message;
      } catch (error) {
        errorMessage.value = error.message;
      }
}

function verifyRegister(){
    errorMessage.value = '';

    if(email.value === '' || password.value === '' || username.value === '') {
        errorMessage.value = 'Please fill in all fields';
    }
    else if(password.value.length < 8) {
        errorMessage.value = 'Password must be at least 8 characters';
    }
    else if(!email.value.includes('@') || !email.value.includes('.')) {
        errorMessage.value = 'Invalid email address';
    }
    if(errorMessage.value === '') {
        handleRegister();
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
            <button class="submit-button" type="submit">Register</button>
        </form>
        <p class="default-text">{{ errorMessage }}</p>
    </div>
</template>

<style scoped>
</style>
