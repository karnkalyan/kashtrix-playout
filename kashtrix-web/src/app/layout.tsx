import type { Metadata } from "next";
import { Plus_Jakarta_Sans } from "next/font/google";
import { ThemeProvider } from "@/components/ThemeProvider";
import "./globals.css";

const jakarta = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700", "800"],
  variable: "--font-jakarta"
});

export const metadata: Metadata = {
  title: "Kashtrix | Enterprise Broadcast Playout & Media Software Suite",
  description:
    "Enterprise 24/7 TV Broadcast Playout, Live Production Switcher, 3D Character Generator, Media Asset Management, Ingest, NRCS/MOS & SMPTE ST 2110 IP Transmission.",
  keywords: [
    "Broadcast Playout",
    "Kashtrix",
    "TV Automation",
    "Character Generator",
    "MOS 2.8.4",
    "SMPTE ST 2110",
    "NDI 6",
    "Media Asset Management",
    "Teleprompter",
    "Live Production Switcher",
    "StreamOps",
    "OSS BSS"
  ],
  authors: [{ name: "Kashtrix Broadcast Systems" }],
  icons: {
    icon: "/favicon.ico"
  }
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className={`light ${jakarta.variable}`} suppressHydrationWarning>
      <body className="min-h-screen antialiased selection:bg-[#7C3AED] selection:text-white transition-colors duration-200">
        <ThemeProvider>
          {children}
        </ThemeProvider>
      </body>
    </html>
  );
}
