export async function customFetch<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  const baseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";
  const response = await fetch(`${baseUrl}${url}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
    // Deaktiver Next.js-caching for å alltid hente fersk data
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`API-feil: ${response.status} ${response.statusText}`);
  }

  // 204 No Content har ingen body
  if (response.status === 204) return undefined as T;
  return response.json();
}
