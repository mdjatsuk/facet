import { useAuth } from "~/composables/useAuth";

export default defineNuxtRouteMiddleware((to, from) => {
  const auth = useAuth();

  const publicPaths = ["/login", "/register"];

  if (process.client) {
    if (!auth.isAuthenticated.value && !publicPaths.includes(to.path)) {
      return navigateTo("/login");
    }

    if (auth.isAuthenticated.value && publicPaths.includes(to.path)) {
      return navigateTo("/");
    }
  }
});
