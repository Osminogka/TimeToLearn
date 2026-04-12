<script setup>
import { reactive, ref } from 'vue';
import { user } from '@/Shared/services/utils';
import { useBackNavigation } from '@/Shared/services/useBackNavigation';

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

const { goBack } = useBackNavigation('Main');
</script>

<template>
  <main class="setup-shell">
    <section class="setup-card surface-card surface-card--raised">
      <header class="setup-header">
        <button class="secondary-button setup-back" type="button" @click="goBack">Back</button>
        <p class="section-kicker">Profile setup</p>
        <h1 class="section-title">Welcome, {{ user.name }}</h1>
        <p class="section-copy">Complete a few details so your university profile feels more personal and useful.</p>
      </header>

      <div class="setup-grid">
        <div class="field-group setup-name-grid">
          <label class="field-label">Name</label>
          <div class="setup-name-grid__inputs">
            <input type="text" v-model="firstName" class="input-field" placeholder="First name" />
            <input type="text" v-model="lastName" class="input-field" placeholder="Last name" />
          </div>
        </div>

        <div class="field-group">
          <label class="field-label">Contact Information</label>
          <input v-model="phone" type="text" class="input-field" placeholder="Phone number" />
        </div>

        <div class="field-group">
          <label class="field-label">Address</label>
          <div class="setup-address-grid">
            <input v-model="address.country" type="text" class="input-field" placeholder="Country" />
            <div class="setup-name-grid__inputs">
              <input v-model="address.city" type="text" class="input-field" placeholder="City" />
              <input v-model="address.street" type="text" class="input-field" placeholder="Street" />
            </div>
          </div>
        </div>

        <div class="field-group">
          <label class="field-label">Role</label>
          <select v-model="role" class="input-field custom-select">
            <option value="student">Student</option>
            <option value="teacher">Teacher</option>
          </select>
        </div>

        <button class="submit-button setup-submit" @click="submit()">Complete setup</button>
      </div>
    </section>
  </main>
</template>

<style scoped>
.setup-shell {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.25rem;
}

.setup-card {
  width: min(760px, 100%);
  padding: clamp(1.25rem, 3vw, 2rem);
}

.setup-header {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin-bottom: 1.35rem;
  text-align: left;
}

.setup-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.setup-name-grid,
.setup-address-grid {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.setup-name-grid__inputs {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.85rem;
}

.custom-select {
  appearance: none;
}

.setup-submit {
  margin-top: 0.35rem;
}

.setup-back {
  width: fit-content;
}

@media (max-width: 640px) {
  .setup-name-grid__inputs {
    grid-template-columns: 1fr;
  }
}
</style>