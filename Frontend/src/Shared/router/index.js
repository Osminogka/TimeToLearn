import { createRouter, createWebHistory } from 'vue-router';
import { isAuthenticated } from '@/Shared/services/utils';

const routes = [
  {
    path: '/login',
    name: "Login",
    component: () => import("@/Authentication/components/LoginPage.vue"), // Login page
    meta: { 
        title : "Login",
        requiresAuth: false 
    }
  },
  {
    path: '/register',
    name: "Register",
    component: () => import("@/Authentication/components/RegisterPage.vue"), // Register page
    meta: { 
        title : "Register",
        requiresAuth: false 
    }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// Navigation Guard
router.beforeEach((to, from, next) => {
  const isAuth = isAuthenticated(); // Check if the user is authenticated

  if (to.matched.some((record) => record.meta.requiresAuth)) {
    // Redirect to login if route requires authentication and user is not authenticated
    if (!isAuth) {
      next('/');
    } else {
      next();
    }
  } else {
    // Redirect authenticated users away from public routes
    if (isAuth && (to.path === '/login' || to.path === '/register')) {
      to.path = '/'
      next('/');
    } else {
      next();
    }
  }
});

router.beforeEach((to, from, next) => {
  document.title = to.meta.title || 'Time to Learn';
  next();
});

export default router;
