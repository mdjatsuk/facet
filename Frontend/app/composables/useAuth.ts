import type { User } from "../types/user";
import type { NitroFetchOptions, NitroFetchRequest } from "nitropack";
import { computed, watch } from 'vue'

export const useAuth = () => {
  const STORAGE_TOKEN_KEY = 'facet_token'
  const STORAGE_USERNAME_KEY = 'facet_username'

  const activeToken = useState<string | undefined>("token", () => undefined);
  const currentUsername = useState<string | null>("username", () => null);

  if (process.client) {
    try {
      const saved = localStorage.getItem(STORAGE_TOKEN_KEY)
      if (saved && !activeToken.value) activeToken.value = saved
      const savedUser = localStorage.getItem(STORAGE_USERNAME_KEY)
      if (savedUser && !currentUsername.value) currentUsername.value = savedUser
    } catch (e) {
    }

    watch(activeToken, (val) => {
      try {
        if (val) localStorage.setItem(STORAGE_TOKEN_KEY, val)
        else localStorage.removeItem(STORAGE_TOKEN_KEY)
      } catch (e) {}
    })

    watch(currentUsername, (val) => {
      try {
        if (val) localStorage.setItem(STORAGE_USERNAME_KEY, val)
        else localStorage.removeItem(STORAGE_USERNAME_KEY)
      } catch (e) {}
    })
  }

  const api = useApi();

  const isAuthenticated = computed(() => !!activeToken.value);

  const logIn = async (user: User) => {
    try {
      const tokenResp = await api.post<any>("api/users", user);
      const jwt = typeof tokenResp === 'string' ? tokenResp : tokenResp?.token;
      if (jwt) {
        activeToken.value = jwt;
        const providedName = (user as any).username || (user as any).Username || null;
        currentUsername.value = providedName;
        return true;
      }
      return false;
    } catch (e) {
      // console.error('Login failed', e)
      return false;
    }
  };

  const logOut = async () => {
    activeToken.value = undefined;
    currentUsername.value = null;
    if (process.client) {
      try {
        localStorage.removeItem(STORAGE_TOKEN_KEY)
        localStorage.removeItem(STORAGE_USERNAME_KEY)
      } catch (e) {}
    }
    return navigateTo('/login')
  };

  const register = async (user: User) => {
    try {
      const res = await api.post<any>("api/users/register", user);
      if (res) {
        const ok = await logIn(user);
        return ok;
      }
      return false;
    } catch (e) {
      return false;
    }
  };

  const fetchWithToken = async <T>(
    url: string,
    options?: NitroFetchOptions<NitroFetchRequest>
  ) => {
    return await api.customFetch<T>(url, {
      ...options,
      headers: {
        Authorization: "Bearer " + activeToken.value,
        ...options?.headers,
      },
    });
  };

  return { logIn, logOut, isAuthenticated, fetchWithToken, register, currentUsername };
};
