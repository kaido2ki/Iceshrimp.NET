import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import './style.css';
import AppSkeleton from "./App.vue";
import AuthPage from "./pages/auth.vue";

const routes = [
    { path: '/', component: AuthPage }
];

const router = createRouter({
    history: createWebHistory('/'),
    routes,
})

const app = createApp(AppSkeleton);
app.use(router);
app.mount('#app')
