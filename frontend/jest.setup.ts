import "@testing-library/jest-dom";

// Lenis ships ESM-only; stub it for Jest (jsdom doesn't run real scroll anyway).
jest.mock("lenis/react", () => ({
  ReactLenis: ({ children }: { children: React.ReactNode }) => children,
  useLenis: () => undefined,
}));
