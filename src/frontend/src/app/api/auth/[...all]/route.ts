import { auth } from "@/lib/auth";
import { toNextJsHandler } from "better-auth/next-js";

// Håndterer alle better-auth API-forespørsler under /api/auth/*.
export const { GET, POST } = toNextJsHandler(auth);
