import { createWebHistory, createRouter } from 'vue-router'

import LoginView from './views/security/LoginView.vue'
import CallbackView from './views/security/CallbackView.vue'
import OldHomeView from './views/HomeView.vue'
import AppView from './views/Get2View.vue'
import AppOldView from './views/GetView.vue'
import EditView from './views/EditView.vue'
import DocsView from './views/DocsView.vue'
import PoCView from './views/PoCView.vue'
import RepoView from './views/ReposView.vue'
import GithubView from './views/GithubView.vue'
import UsersView from './views/UsersView.vue'
import ErrorView from './views/ErrorView.vue'
import HelpView from './views/HelpView.vue'
import AccountView from './views/AccountView.vue'
import UnauthorizedView from './views/UnauthorizedView.vue'
import NotFoundView from './views/NotFoundView.vue'

const routes = [
    { path: '/', alias: ['/home', '/index', '/github'], name: 'home', component: GithubView, meta: { requiresAuth: true } },
    { path: '/old', name: 'old', component: OldHomeView, meta: { requiresAuth: true } },
    { path: '/login', name: 'login', component: LoginView, meta: { requiresAuth: false } },
    { path: '/callback', name: 'callback', component: CallbackView },
    { path: '/user', alias: ['/account', '/idme'], name: 'account', component: AccountView, meta: { requiresAuth: true } },
    { path: '/app/get/:id', name: 'get', component: AppView, meta: { requiresAuth: true } },
    { path: '/app/getold/:id', name: 'getold', component: AppOldView, meta: { requiresAuth: true } },
    { path: '/app/insert', alias: ['/add'], name: 'insert', component: EditView, meta: { requiresAuth: true } },
    { path: '/app/update/:id', name: 'update', component: EditView, meta: { requiresAuth: true } },
    { path: '/docs', alias: ['/lib'], name:'docs', component: DocsView, meta: { requiresAuth: true } },
    { path: '/pocs', name:'pocs', component: PoCView, meta: { requiresAuth: true } },
    { path: '/repo', name:'repo', component: RepoView, meta: { requiresAuth: true } },
    { path: '/users', name:'users', component: UsersView, meta: { requiresAuth: true } },
    { path: '/help', name:'help', component: HelpView, meta: { requiresAuth: false } },
    { path: '/error', name:'error', component: ErrorView, meta: { requiresAuth: false } },
    { path: '/401', name:'401', component: UnauthorizedView, meta: { requiresAuth: false } },
    { path: '/logout', alias: ['/bye', '/signout', '/signoff'], name: 'logout' },
    { path: '/erase', alias: ['/clear'], name: 'erase' },
    { path: '/:pathMatch(.*)*', component: NotFoundView, meta: { requiresAuth: false } }
]

const router = createRouter({
    history: createWebHistory(),
    routes,
})

export default router
