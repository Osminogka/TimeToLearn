import { createRouter, createWebHistory } from 'vue-router';
import { isAuthenticated } from '@/Shared/services/utils';

const routes = [
    {
        path: '/',
        name: "Main",
        component: () => isAuthenticated()
            ? import('@/Core/universities/pages/DashboardUniversties.vue')
            : import('@/Shared/pages/GreetingPage.vue'),
        meta: {
            title: "Time to Learn",
            requiresAuth: false
        },
        children: [
            {
                path: 'login',
                name: 'Login',
                component: () => import("@/Authentication/pages/LoginPage.vue"),
                meta: {
                    guestOnly: true,
                    title: 'Login'
                }
            },
            {
                path: 'register',
                name: 'Register',
                component: () => import("@/Authentication/pages/RegisterPage.vue"),
                meta: {
                    guestOnly: true,
                    title: 'Register'
                }
            }
        ]
    },
    {
        path: '/define-role',
        name: 'DefineRole',
        component: () => import('@/Core/roles/pages/DefineRole.vue'),
        meta: {
            title: "Define Role",
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
    const isAuth = isAuthenticated();

    if (to.meta.guestOnly && isAuth) {
        return next({ name: 'Main' });
    }

    if (to.meta.requiresAuth && !isAuth) {
        return next({ name: 'Login' });
    }

    next();
});

router.beforeEach((to, from, next) => {
    document.title = to.meta.title || 'Time to Learn';
    next();
});

export default router;
