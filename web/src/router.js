import { createRouter, createWebHistory } from 'vue-router'
import FoundationView from './views/FoundationView.vue'
import DashboardView from './views/DashboardView.vue'
import GitHubDataView from './views/GitHubDataView.vue'

const routes = [
  { path: '/', redirect: '/foundation' },
  { path: '/foundation', name: 'foundation', component: FoundationView },
  { path: '/dashboard', name: 'dashboard', component: DashboardView },
  { path: '/github-data', name: 'github-data', component: GitHubDataView },
  { path: '/:pathMatch(.*)*', redirect: '/foundation' },
]

export default createRouter({ history: createWebHistory(), routes })
