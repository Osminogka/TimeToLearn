import { createRouter, createWebHistory } from 'vue-router';
import { isAuthenticated } from '@/Authentication/services/utils';

const routes = [
  {
    path: '/',
    name: "GreetingPage",
    component: () => import("@/components/GreetingPage.vue"), // Public greeting page
    children: [
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
    ],
    meta: { 
        title : "Greeting",
        requiresAuth: false 
    }
  },
  {
    path: '/dashboard',
    name: "Dashboard",
    component: () => import("@/components/Dashboard.vue"), // Main functionality
    meta: { 
        title : "Dashboard",
        requiresAuth: true 
    }
  },
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
      next('/login');
    } else {
      next();
    }
  } else {
    // Redirect authenticated users away from public routes
    if (isAuth && (to.path === '/login' || to.path === '/register')) {
      next('/dashboard');
    } else {
      next();
    }
  }
});

export default router;
