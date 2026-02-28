# My Project Guidelines — Elementum

These are my rules for working in this repository: I want a professional, traceable, and maintainable codebase that works well for my portfolio and for collaboration.

---

## 1. Git & Branching

### Structured Workflow
- **I protect `main`:** I develop features and fixes in dedicated branches and only merge into `main` after review or via Pull Request.
- **Branch naming:** I start with the **project name**, then the type and a short description. The projects in this repo are largely independent, so organizing by project keeps my branches clear:
  - `elementum-service/feature/api-auth-implementation`
  - `elementum-service/fix/frontend-routing`
  - `elementum-database/refactor/worker-logic`
  - `elementum-service/docs/readme-setup`

### Pull Requests
I use Pull Requests even when working alone, so I document my intent and decisions:

1. I push my branch (e.g. `elementum-service/feature/new-worker-logic`).
2. I open a **Pull Request** on GitHub from that branch into `main`.
3. In the PR description, I include:
   - *What did I change?*
   - *Why did I implement it this way?*
   - *What challenges did I run into?*
4. I merge the PR once it’s ready.

That way the Pull Requests tab (including closed ones) is a clear record of my work and reasoning—useful for reviews and portfolio.

### Commit Messages
- **I avoid:** Vague messages like "update", "fix", "test", or "changes".
- **I prefer:** Conventional, descriptive messages, for example:
  - `feat: add JWT authentication to API`
  - `fix: resolve null reference in worker`
  - `refactor: optimize database query in worker`
  - `docs: update README with API startup`
  - `chore: bump dependency xyz`

I keep each message to one line and make *what* changed (and *why* when it matters) clear.

---

## 2. Documentation

### README.md
My README includes at least:

- **Project purpose:** What the project does, who it’s for, and the context.
- **Getting started:**
  - How to run the API.
  - How to run the frontend.
  - How to run workers or background jobs (if applicable).
- **(Optional)** A simple diagram of how API, frontend, workers, and database interact.

For this monorepo: I briefly describe the folder structure and the role of each major part.

### Code Comments
- I comment non-obvious logic and non-trivial design decisions.
- I don’t comment the obvious (e.g. "sets variable x").
- I document public APIs, interfaces, and endpoints with clear descriptions.

---

## 3. Code Quality & Consistency

### Consistent Style
- **One repo, one standard:** I keep naming, structure, and formatting consistent across frontend, API, and workers.

### Clean Code
- I prioritize readability over cleverness.
- I keep functions and modules short and focused.
- I remove dead code and unused imports or variables.

### Reuse
- I don’t duplicate shared logic (e.g. validation, configuration). I extract it into shared modules or libraries.

---

## 4. Security & Configuration

- **No secrets in the repo:** I never commit API keys, passwords, or connection strings. I use environment variables or secure config (e.g. `.env` in `.gitignore`).
- **Example config:** I provide a `*.example` file or a README section that lists required variables—without real values.

---

## 5. Testing (Where Applicable)

- I add unit or integration tests for critical paths and business logic.
- I keep tests stable and reproducible (no flaky tests).
- I document in the README how to run the test suite.

---

## 6. Dependencies & Versions

- I pin dependency versions (e.g. in `package.json`, `*.csproj`) or use lockfiles so builds are reproducible.
- I periodically check for outdated or vulnerable packages (e.g. `npm audit`, NuGet security tools).

---

## 7. My Repository Quality Checklist

- [ ] Descriptive commit messages (e.g. feat / fix / refactor / docs)
- [ ] README with project description, setup, and optional architecture diagram
- [ ] Consistent, clean code across all parts of the monorepo
- [ ] No secrets in the repo; config via environment variables documented
- [ ] Branches and PRs with clear names and descriptions
- [ ] Optional: keep closed PRs visible as a traceable history of changes

---
