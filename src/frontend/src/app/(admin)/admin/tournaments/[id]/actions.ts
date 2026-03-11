"use server";
import { revalidatePath } from "next/cache";

// API-basis-URL — hentes fra miljøvariabel eller faller tilbake til localhost
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Oppdaterer poengregler for en turnering via PUT /api/v1/tournaments/:id/point-rules
export async function updatePointRulesAction(
  tournamentId: string,
  formData: FormData
) {
  const body = {
    tournamentId,
    participantPoints: Number(formData.get("participantPoints")),
    firstPlacePoints: Number(formData.get("firstPlacePoints")),
    secondPlacePoints: Number(formData.get("secondPlacePoints")),
    thirdPlacePoints: Number(formData.get("thirdPlacePoints")),
    organizerWithParticipationPoints: Number(
      formData.get("organizerWithParticipationPoints")
    ),
    organizerWithoutParticipationPoints: Number(
      formData.get("organizerWithoutParticipationPoints")
    ),
    spectatorPoints: Number(formData.get("spectatorPoints")),
  };

  const res = await fetch(
    `${API_BASE}/api/v1/tournaments/${tournamentId}/point-rules`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }
  );

  if (!res.ok) throw new Error("Kunne ikke oppdatere poengregler");
  revalidatePath(`/admin/tournaments/${tournamentId}`);
}

// Oppretter et nytt spill i turneringen via POST /api/v1/games
export async function createGameAction(
  tournamentId: string,
  formData: FormData
) {
  const body = {
    tournamentId,
    name: formData.get("name") as string,
    gameType: formData.get("gameType") as string,
    isOrganizersParticipating:
      formData.get("isOrganizersParticipating") === "on",
  };

  const res = await fetch(`${API_BASE}/api/v1/games`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!res.ok) throw new Error("Kunne ikke opprette spill");
  revalidatePath(`/admin/tournaments/${tournamentId}`);
}
