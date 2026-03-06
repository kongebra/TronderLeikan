import { defineConfig } from "orval";

export default defineConfig({
  tronderleikan: {
    input: {
      target: process.env.SWAGGER_URL ?? "http://localhost:5000/swagger/v1/swagger.json",
    },
    output: {
      target: "./src/lib/api/index.ts",
      schemas: "./src/lib/api/model",
      client: "fetch",
      override: {
        mutator: {
          path: "./src/lib/api/fetcher.ts",
          name: "customFetch",
        },
      },
    },
  },
});
