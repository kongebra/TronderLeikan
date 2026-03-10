"use server";
import { revalidatePath } from "next/cache";

// API-basis-URL — hentes fra miljøvariabel eller faller tilbake til localhost
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Legger til en deltaker i et spill via POST /api/v1/games/:gameId/participants
export async function addParticipantAction(
  gameId: string,
  personId: string,
  tournamentId: string
) {
  await fetch(`${API_BASE}/api/v1/games/${gameId}/participants`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ personId }),
  });
  revalidatePath(`/admin/tournaments/${tournamentId}/games/${gameId}`);
}

// Fullfører et spill med plasseringer via POST /api/v1/games/:gameId/complete
export async function completeGameAction(
  gameId: string,
  tournamentId: string,
  formData: FormData
) {
  const firstPlace = formData.getAll("firstPlace") as string[];
  const secondPlace = formData.getAll("secondPlace") as string[];
  const thirdPlace = formData.getAll("thirdPlace") as string[];

  const res = await fetch(`${API_BASE}/api/v1/games/${gameId}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ gameId, firstPlace, secondPlace, thirdPlace }),
  });

  if (!res.ok) throw new Error("Kunne ikke fullføre spill");
  revalidatePath(`/admin/tournaments/${tournamentId}/games/${gameId}`);
}
