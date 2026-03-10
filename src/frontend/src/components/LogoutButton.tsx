"use client";

import { authClient } from "@/lib/auth-client";
import { useRouter } from "next/navigation";

// Utloggingsknapp — klientkomponent siden den kaller authClient.signOut()
export function LogoutButton() {
  const router = useRouter();

  async function handleSignOut() {
    await authClient.signOut({
      fetchOptions: {
        onSuccess: () => {
          router.push("/login");
        },
      },
    });
  }

  return (
    <button
      onClick={handleSignOut}
      className="admin-logout-btn"
      aria-label="Logg ut"
    >
      <span className="admin-logout-icon" aria-hidden="true">
        <svg
          width="14"
          height="14"
          viewBox="0 0 14 14"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M5 2H2.5C2.22386 2 2 2.22386 2 2.5V11.5C2 11.7761 2.22386 12 2.5 12H5"
            stroke="currentColor"
            strokeWidth="1.25"
            strokeLinecap="round"
          />
          <path
            d="M9 4.5L11.5 7L9 9.5"
            stroke="currentColor"
            strokeWidth="1.25"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d="M5.5 7H11.5"
            stroke="currentColor"
            strokeWidth="1.25"
            strokeLinecap="round"
          />
        </svg>
      </span>
      <span>Logg ut</span>
    </button>
  );
}
