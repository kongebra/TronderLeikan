"use server";
import { revalidatePath } from "next/cache";

// API-basis-URL — hentes fra miljøvariabel eller faller tilbake til localhost
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Oppretter en ny spiller via POST /api/v1/persons
export async function createPersonAction(formData: FormData) {
  const firstName = formData.get("firstName") as string;
  const lastName = formData.get("lastName") as string;

  const res = await fetch(`${API_BASE}/api/v1/persons`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ firstName, lastName }),
  });

  if (!res.ok) throw new Error("Kunne ikke opprette spiller");
  revalidatePath("/admin/persons");
}

// Sletter en spiller via DELETE /api/v1/persons/:id
export async function deletePersonAction(id: string) {
  const res = await fetch(`${API_BASE}/api/v1/persons/${id}`, {
    method: "DELETE",
  });

  if (!res.ok) throw new Error("Kunne ikke slette spiller");
  revalidatePath("/admin/persons");
}
