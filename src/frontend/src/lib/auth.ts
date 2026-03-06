import { betterAuth } from "better-auth";
import { genericOAuth } from "better-auth/plugins";

// Konfigurerer better-auth med Zitadel som OIDC-leverandør via genericOAuth-plugin.
// discoveryUrl brukes til å hente endepunkter automatisk fra Zitadels OIDC-metadata.
// Miljøvariabler settes av Aspire ved kjøretid.
export const auth = betterAuth({
  secret: process.env.BETTER_AUTH_SECRET!,
  baseURL: process.env.BETTER_AUTH_URL ?? "http://localhost:3000",
  plugins: [
    genericOAuth({
      config: [
        {
          providerId: "zitadel",
          // Zitadel sitt OIDC-discovery-endepunkt
          discoveryUrl: `${process.env.ZITADEL_ISSUER}/.well-known/openid-configuration`,
          issuer: process.env.ZITADEL_ISSUER,
          clientId: process.env.ZITADEL_CLIENT_ID!,
          clientSecret: process.env.ZITADEL_CLIENT_SECRET!,
          scopes: ["openid", "profile", "email"],
          pkce: true,
        },
      ],
    }),
  ],
});
