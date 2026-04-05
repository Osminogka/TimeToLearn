import { createApp } from 'vue'
import router from './Shared/router'
import App from './App.vue'
import './Shared/styles/index.css'

createApp(App)
.use(router)
.mount('#app')
