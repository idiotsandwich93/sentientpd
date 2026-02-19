# CLAUDE.md

This file provides guidance for AI assistants (and contributors) working in this repository.

## Project Overview

**sentientpd** is a project in early initialization. This repository currently contains only a README and no implementation code, tests, or build configuration.

## Repository Structure

```
sentientpd/
├── README.md    # Project title
└── CLAUDE.md    # This file
```

As the project grows, update this section to reflect the actual directory layout.

## Development Workflow

### Branches

- `master` — stable, production-ready code
- `claude/<session-id>` — AI-assisted feature branches (auto-generated per session)

Always develop on a feature branch and open a pull request before merging to `master`.

### Commits

Write commit messages in the imperative mood and keep the subject line under 72 characters:

```
Add user authentication module
Fix null pointer in session handler
Refactor database connection pooling
```

### Pull Requests

- Keep PRs focused on a single concern
- Include a short summary of what changed and why
- Reference any related issues

## Conventions for AI Assistants

- **Read before editing.** Always read a file before modifying it.
- **Minimal changes.** Only change what is directly requested. Do not refactor, add comments, or clean up unrelated code.
- **No speculative features.** Do not add configuration options, error handling, or abstractions for hypothetical future requirements.
- **No new files unless necessary.** Prefer editing existing files. Never create documentation files unless explicitly asked.
- **Security.** Do not introduce command injection, XSS, SQL injection, or other OWASP Top 10 vulnerabilities.
- **Commit and push.** When implementing a change, commit with a descriptive message and push to the designated branch.

## Getting Started

This section should be updated once the project technology stack is chosen. Typical steps will include:

1. Clone the repository
2. Install dependencies (e.g., `npm install`, `pip install -r requirements.txt`)
3. Copy environment configuration (e.g., `cp .env.example .env`)
4. Run the development server or build process
5. Run tests to verify the setup

## Testing

No testing framework has been configured yet. When one is added, document the commands here (e.g., `npm test`, `pytest`, `go test ./...`) and describe the test structure and any coverage requirements.

## Key Files to Update as the Project Evolves

| File | Purpose |
|------|---------|
| `README.md` | User-facing project description, install instructions, usage examples |
| `CLAUDE.md` | AI assistant and contributor guidance (this file) |
| `.env.example` | Template for environment variables (never commit real secrets) |
| Package/dependency manifest | Language-specific dependency list |
