<script setup>
import { useRouter } from 'vue-router';

const router = useRouter();

function goToGreetings() {
  router.push("/");
}

function isAuthRoute() {
  return router.currentRoute.value.name === 'Login' || router.currentRoute.value.name === 'Register';
}

</script>

<template>
  <header>
      <h1 class="gradient-title" @click="goToGreetings">Time to Learn!</h1>
      <div class="register-buttons">
        <router-link :to="{ name: 'Login' }" class="redirect-button">Login</router-link>
        <router-link :to="{ name: 'Register' }" class="redirect-button">Register</router-link>
      </div>
  </header>
  <main>
    <Transition name="slide-left-right">
      <router-view class="block" v-slot="{ Component }">
        <component :is="Component" />
      </router-view>
    </Transition>
    <Transition name="slide-left-right">
      <div class="block" v-if="!isAuthRoute()">
        <div class="info-card default-text">
          <h1>University Access</h1>
          <p>
            Users can join a variety of universities available on the platform, each with its own unique content and features.
          </p>
        </div>
        <div class="info-card default-text">
          <h1>Collaboration and Community</h1>
          <p>
            Users can connect with peers within their university communities to foster learning and networking.
          </p>
        </div>
        <div class="info-card default-text">
          <h1>Forums</h1>
          <p>
            Each university includes a forum system similar to Reddit, allowing users to:
            <ul>
              <li>Share ideas, resources, and ask questions.</li>
              <li>Upvote or downvote posts and comments.</li>
              <li>Create and participate in discussions.</li>
            </ul>
          </p>
        </div>
        <div class="info-card default-text">
          <h1>Mobile and Desktop Friendly</h1>
          <p>
            The platform is designed to be responsive and user-friendly on both mobile and desktop devices.
          </p>
        </div>
      </div>
    </Transition>
  </main>
</template>

<style scoped>
@import '@/assets/css/text-classes.css';
@import '@/assets/css/transitions.css';

main{
  display: flex;
  justify-content: center;
}

header {
  padding: 1rem;
  display: flex;
  align-items: center;
  background-color: #1C1C1C;
}

.register-buttons{
  display: flex;
  flex-direction: column;
}

.redirect-button{
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

.redirect-button:hover{
  background-color: #6f0672;
}

.info-card{
  background-color: #1C1C1C;
  border-radius: 2rem;
  border-color: #B344C6;
  border-width: 1px;
  border-style: solid;
  padding: 1rem;
  margin-top: 0.5rem;
  margin-bottom: 0.5rem;
  border-radius: 5px;
}

.block {
  padding: auto;
  width: 90%;
  display: flex;
  flex-direction: column;
  position: absolute;
  top: 10;
}

</style>