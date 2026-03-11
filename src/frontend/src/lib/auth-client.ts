import { createAuthClient } from "better-auth/client";

// Klientside-instans for better-auth.
// NEXT_PUBLIC_BETTER_AUTH_URL settes av Aspire ved kjøretid.
export const authClient = createAuthClient({
  baseURL: process.env.NEXT_PUBLIC_BETTER_AUTH_URL ?? "http://localhost:3000",
});
