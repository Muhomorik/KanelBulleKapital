export function Footer() {
  return (
    <footer className="mt-auto border-t border-border/60 py-6">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="flex flex-col items-center justify-between gap-2 text-xs text-muted-foreground sm:flex-row">
          <p>
            <span className="font-serif font-medium text-foreground">
              SmorgasBoard
            </span>{" "}
            — AI-powered market intelligence by{" "}
            <a
              href="https://github.com/Muhomorik/KanelBulleKapital"
              target="_blank"
              rel="noopener noreferrer"
              className="underline underline-offset-2 transition-colors hover:text-foreground"
            >
              KanelBulleKapital
            </a>
          </p>
          <p>
            Built with Azure AI Foundry + Microsoft Agent Framework
          </p>
        </div>
      </div>
    </footer>
  );
}
