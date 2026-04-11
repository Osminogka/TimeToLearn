import { createRouter, createWebHistory } from 'vue-router';
import { isAuthenticated } from '@/Shared/services/utils';

const routes = [
    {
        path: '/',
        name: "Main",
        component: () => import('@/Shared/pages/GreetingPage.vue'),
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
        path: '/dashboard',
        name: 'Dashboard',
        component: () => import('@/Core/universities/pages/DashboardUniversties.vue'),
        meta: {
            title: 'Dashboard',
            requiresAuth: true
        }
    },
    {
        path: '/define-role',
        name: 'DefineRole',
        component: () => import('@/Core/roles/pages/DefineRole.vue'),
        meta: {
            title: "Define Role",
            requiresAuth: false
        }
    },
    {
        path: '/universities',
        name: 'UniversitiesAll',
        component: () => import('@/Core/universities/pages/UniversitiesCatalogPage.vue'),
        meta: {
            title: 'All Universities',
            requiresAuth: true
        }
    },
    {
        path: '/universities/my',
        name: 'UniversitiesMine',
        component: () => import('@/Core/universities/pages/MyUniversitiesPage.vue'),
        meta: {
            title: 'My Universities',
            requiresAuth: true
        }
    },
    {
        path: '/universities/create',
        name: 'UniversitiesCreate',
        component: () => import('@/Core/universities/pages/CreateUniversityPage.vue'),
        meta: {
            title: 'Create University',
            requiresAuth: true
        }
    },
    {
        path: '/account',
        name: 'AccountManagement',
        component: () => import('@/Users/pages/AccountManagementPage.vue'),
        meta: {
            title: 'Account Management',
            requiresAuth: true
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

    if (to.name === 'Main' && isAuth) {
        return next({ name: 'Dashboard' });
    }

    if (to.meta.guestOnly && isAuth) {
        return next({ name: 'Dashboard' });
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
