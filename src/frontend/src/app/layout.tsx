import type { Metadata } from "next";
import { Sora } from "next/font/google";
import Link from "next/link";
import "./globals.css";

// Sora brukes som hoved-display-font — kraftig og moderne, passer godt til
// en turnerings- og poengplattform med nordisk karakter
const sora = Sora({
  variable: "--font-sora",
  subsets: ["latin"],
  weight: ["300", "400", "600", "700", "800"],
  display: "swap",
});

export const metadata: Metadata = {
  title: {
    default: "TrønderLeikan",
    template: "%s — TrønderLeikan",
  },
  description:
    "Plattform for turneringsstyring og poengberegning i Trøndelag. Følg med på rangeringer, resultater og spillerstatistikk.",
  metadataBase: new URL("https://tronderleikan.no"),
};

// Navigasjonslenker — legges i en konstant for enkel utvidelse
const navLinks = [
  { href: "/", label: "Turneringer" },
  { href: "/players", label: "Spillere" },
] as const;

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="no" className="dark">
      <body className={`${sora.variable} font-sans antialiased`}>
        {/* Bakgrunnstekstur — subtil støy-overlay for dybde */}
        <div className="noise-overlay" aria-hidden="true" />

        {/* Toppnavigasjon */}
        <header className="site-header">
          <div className="header-inner">
            {/* Logo / merkenavn */}
            <Link href="/" className="site-logo">
              <span className="logo-mark" aria-hidden="true">⬡</span>
              <span className="logo-text">TrønderLeikan</span>
            </Link>

            {/* Primærnavigasjon */}
            <nav className="site-nav" aria-label="Primærnavigasjon">
              <ul className="nav-list" role="list">
                {navLinks.map(({ href, label }) => (
                  <li key={href}>
                    <Link href={href} className="nav-link">
                      {label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          </div>
        </header>

        {/* Horisontalt skillelinje med aksent-gradient */}
        <div className="header-rule" aria-hidden="true" />

        {/* Sideinnhold */}
        <main className="site-main">{children}</main>

        {/* Bunntekst */}
        <footer className="site-footer">
          <div className="footer-inner">
            <span className="footer-brand">TrønderLeikan</span>
            <span className="footer-divider" aria-hidden="true">·</span>
            <span className="footer-tagline">
              Turneringer og poengberegning i Trøndelag
            </span>
          </div>
        </footer>
      </body>
    </html>
  );
}
