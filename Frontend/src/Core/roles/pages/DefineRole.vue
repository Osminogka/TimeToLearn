<script setup>
import { reactive, ref } from 'vue';
import { user } from '@/Shared/services/utils';

const firstName = ref('');
const lastName = ref('');
const phone = ref('');
const role = ref('student');
const address = reactive({
    country: '',
    city: '',
    street: ''
});

const submit = () => {
    console.log("Form Submitted");
};
</script>

<template>
  <div class="page-wrapper">
    <div class="form-container">
      <header class="form-header">
        <h2 class="gradient-title">Welcome, {{ user.name }}</h2>
        <p class="subtitle">Please provide more information about you</p>
      </header>

      <div class="form-grid">
        <div class="form-section">
          <label class="field-label">Name</label>
          <div class="input-row">
            <input type="text" v-model="firstName" class="input-field" placeholder="First Name" />
            <input type="text" v-model="lastName" class="input-field" placeholder="Last Name" />
          </div>
        </div>

        <div class="form-section">
          <label class="field-label">Contact Information</label>
          <input v-model="phone" type="text" class="input-field full-width" placeholder="Phone" />
        </div>

        <div class="form-section">
          <label class="field-label">Address</label>
          <div class="address-grid">
            <input v-model="address.country" type="text" class="input-field" placeholder="Country" />
            <div class="input-row">
              <input v-model="address.city" type="text" class="input-field" placeholder="City" />
              <input v-model="address.street" type="text" class="input-field" placeholder="Street" />
            </div>
          </div>
        </div>

        <div class="form-section">
          <label class="field-label">Role</label>
          <select v-model="role" class="input-field full-width custom-select">
            <option value="student">Student</option>
            <option value="teacher">Teacher</option>
          </select>
        </div>

        <button class="submit-button" @click="submit()">Complete Setup</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 1. Layout & Background */
.page-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: var(--ttl-bg-page);
  padding: 20px;
  font-family: var(--ttl-font-body);
}

.form-container {
  width: 100%;
  max-width: 500px;
  background: #1a1a24;
  padding: 2.5rem;
  border-radius: 1.25rem;
  border: 1px solid rgba(179, 68, 198, 0.2);
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
}

.gradient-title {
  font-size: 1.8rem;
  font-weight: 800;
  margin-bottom: 0.5rem;
  text-align: center;
}

.subtitle {
  color: #8b8b9d;
  font-size: 0.9rem;
  text-align: center;
  margin-bottom: 2rem;
}

.field-label {
  display: block;
  color: #b344c6;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 0.6rem;
}

.form-grid {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.input-row {
  display: grid;
  grid-template-columns: 1fr 1fr; /* Side-by-side on desktop */
  gap: 1rem;
}

.address-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.input-field {
  width: 100%;
  padding: 0.8rem 1rem;
  background: #252533;
  border: 1px solid #3f3f52;
  border-radius: 0.75rem;
  color: #ffffff;
  font-size: 1rem;
  transition: all 0.2s ease;
  outline: none;
}

.input-field:focus {
  border-color: #b344c6;
  background: #2a2a3d;
  box-shadow: 0 0 0 4px rgba(179, 68, 198, 0.15);
}

.full-width {
  width: 100%;
}

.submit-button {
  width: 100%;
  padding: 1rem;
  background: #810685;
  color: white;
  border: none;
  border-radius: 0.75rem;
  font-weight: 700;
  font-size: 1rem;
  cursor: pointer;
  transition: transform 0.2s, background 0.2s;
  margin-top: 1rem;
}

.submit-button:hover {
  background: #a10ba8;
  transform: translateY(-2px);
}

@media (max-width: 480px) {
  .input-row {
    grid-template-columns: 1fr;
  }
  
  .form-container {
    padding: 1.5rem;
    background: transparent;
    border: none;
    box-shadow: none;
  }
}
</style>