import { createRouter, createWebHistory } from 'vue-router';
import { hasRole, isAuthenticated } from '@/Shared/services/utils';

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
            requiresAuth: true,
            requiresTeacher: true
        }
    },
    {
        path: '/universities/:name/courses',
        name: 'UniversityCourses',
        component: () => import('@/Core/universities/pages/UniversityCoursesPage.vue'),
        meta: {
            title: 'University Courses',
            requiresAuth: true,
            hidePrimaryNavigation: true
        }
    },
    {
        path: '/universities/:name/info',
        name: 'UniversityInfo',
        component: () => import('@/Core/universities/pages/UniversityInfoPage.vue'),
        meta: {
            title: 'University Info',
            requiresAuth: true,
            hidePrimaryNavigation: true
        }
    },
    {
        path: '/universities/:name/forums',
        name: 'UniversityForums',
        component: () => import('@/Core/universities/pages/UniversityForumsPage.vue'),
        meta: {
            title: 'University Forums',
            requiresAuth: true,
            hidePrimaryNavigation: true
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
        return next({ name: 'UniversitiesMine' });
    }

    if (to.meta.guestOnly && isAuth) {
        return next({ name: 'UniversitiesMine' });
    }

    if (to.meta.requiresAuth && !isAuth) {
        return next({ name: 'Login' });
    }

    if (to.meta.requiresTeacher && !hasRole('teacher')) {
        return next({ name: 'UniversitiesMine' });
    }

    next();
});

router.beforeEach((to, from, next) => {
    document.title = to.meta.title || 'Time to Learn';
    next();
});

export default router;
