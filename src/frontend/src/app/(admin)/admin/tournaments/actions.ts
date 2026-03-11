"use server";
import { revalidatePath } from "next/cache";

// API-basis-URL — hentes fra miljøvariabel eller faller tilbake til localhost
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Oppretter en ny turnering via POST /api/v1/tournaments
export async function createTournamentAction(formData: FormData) {
  const name = formData.get("name") as string;
  // Genererer slug fra navn — små bokstaver, mellomrom til bindestrek, fjerner spesialtegn
  const slug = name
    .toLowerCase()
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "");

  const res = await fetch(`${API_BASE}/api/v1/tournaments`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name, slug }),
  });

  if (!res.ok) throw new Error("Kunne ikke opprette turnering");
  revalidatePath("/admin/tournaments");
}
