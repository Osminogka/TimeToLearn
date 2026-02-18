<script setup>
import { getCurrentUser, user } from '../services/utils';

import { onBeforeMount, reactive, ref } from 'vue';

onBeforeMount(() => {
    getCurrentUser();
});

const firstName = ref('');
const lastName = ref('');

const address = reactive({
    country: '',
    city: '',
    street: ''
});

const phone = ref('');
const role = ref('');

async function submit() {
    const data = {
        firstName: firstName.value,
        lastName: lastName.value,
        address: {
            country: address.country,
            city: address.city,
            street: address.street
        },
        phone: phone.value,
        role: role.value
    };

    console.log(JSON.stringify(data));
    // Send data to the backend
}

</script>

<template>
    <div class="container">
        <div class="content">
            <h2 class="gradient-title welcome-message">Welcome {{ user.name }}</h2>
            <p class="default-text">Please provide more information about you</p>
            <label class="default-text">Name</label>
            <div class="name-container">
                <input type="text" v-model="firstName" class="input-field" placeholder="First Name" />
                <input type="text" v-model="lastName" class="input-field" placeholder="Last Name" />
            </div>
            <label class="default-text">Contact Information</label>
            <input v-model="phone" type="text" class="input-field" placeholder="Phone" />
            <label class="default-text">Address</label>
            <div class="address-container">
                <input v-model="address.country" type="text" class="input-field" placeholder="Country" />
                <input v-model="address.city" type="text" class="input-field" placeholder="City" />
                <input v-model="address.street" type="text" class="input-field" placeholder="Street" /> 
            </div>
            <div class="role-container">
                <label class="default-text">Role</label>
                <select v-model="role" class="input-field">
                    <option value="student">Student</option>
                    <option value="teacher">Teacher</option>
                </select>
            </div>
            <button class="submit-button" @click="submit()">Submit</button>
        </div>
    </div>
</template>

<style scoped>
@import '@/assets/css/text-classes.css';

.container {
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    align-items: center;
    margin: auto;
    margin-top: 1rem;
    background-color: #1C1C1C;
    border: 1px solid #B344C6;
    border-radius: 0.5rem;
    width: 90%;
    height: 80vh;
    padding-top: 1rem;
}

.content {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
}

.welcome-message {
    font-size: 1.5rem;
}

.name-container{
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 1rem;
}

.address-container{
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.role-container{
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.input-field{
    background-color: #1C1C1C;
    padding: 0.5rem;
    border-radius: 0.5rem;
    border-color: #B344C6;
    border-width: 1px;
    border-style: solid;
    color: #B344C6;
    width: 15rem;
}

.submit-button{
  padding: 0.7rem;
  margin: 0.5rem;
  border: none;
  border-radius: 5px;
  background-color: #810685;
  font-family: 'Roboto', sans-serif;
  color: black;
  font-weight: 600;
  cursor: pointer;
}
</style>

