"use client";

import { ThemeProvider as NextThemesProvider } from "next-themes";
import { ReactLenis } from "lenis/react";
import type { ReactNode } from "react";

export function ThemeProvider({ children }: { children: ReactNode }) {
  return (
    <NextThemesProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      disableTransitionOnChange
    >
      <ReactLenis root options={{ duration: 1.4, smoothWheel: true }}>
        {children}
      </ReactLenis>
    </NextThemesProvider>
  );
}
