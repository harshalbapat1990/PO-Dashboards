export const appConfig = {
    // The Vite development server proxies /api to localhost:7071. Do not let
    // local builds select a remote API or expose a Functions key to the browser.
    baseURL: import.meta.env.DEV ? '/' : import.meta.env.VITE_baseURL,
    apiKey: import.meta.env.DEV ? '' : import.meta.env.VITE_apiKey,
    environment: import.meta.env.VITE_environment,
    baseTitle: import.meta.env.VITE_baseTitle,
}
