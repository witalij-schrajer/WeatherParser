# Copilot Instructions

## Project guidelines

- The user prefers delegating as much coding and other work as possible to the local `qwen3-coder:30b` model running through Ollama.
- For WeatherParser, follow the "AI-assisted development" section of `AGENTS.md`: where applicable, qwen3-coder:30b generates, changes and fixes code, plans and English documentation, while the orchestrating LLM writes precise briefs, verifies qwen's results, runs the builds and tests, and reviews each step's result. Plans must describe the requirements for every affected class precisely. If the local model is unavailable, report that limitation.
- English is the project's sole maintained language, as instructed on 2026-10-19. Follow `AGENTS.md` and update canonical unsuffixed English documents directly. The former instruction to update German documentation after English changes is superseded; do not recreate translations or `.en.md` copies. The one exception (user decision dated 2026-11-03) is the German translation of the user documentation in `docs/de/`, which is updated before every release as described in `AGENTS.md` "Language rules".
