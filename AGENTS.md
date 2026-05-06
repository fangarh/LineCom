# Global Language

- Always write every user-facing response, status update, explanation, question, and summary in Russian unless the user explicitly requests another language for a specific artifact, code comment, identifier, citation, or quoted text.

# Project Rules

- Treat `vault/Человекочитаемое` as the source of truth for product, architecture, data model, and cross-cutting requirements.
- Do not leave intentional technical debt. Before finishing a task, check for temporary decisions, unfinished markers, security gaps, migration issues, and maintainability risks.
- Backend uses PostgreSQL through Npgsql and Dapper. Entity Framework is not used.
- Database migrations are SQL scripts executed through the DbUp migrator.
- Local FileStorage is the target file-storage approach for this project.
- SEO and GEO requirements must be considered when changing catalog, landing pages, routing, metadata, or public content.

# Context7

Use Context7 MCP to fetch current documentation whenever the user asks about a library, framework, SDK, API, CLI tool, or cloud service, even well-known ones like React, Next.js, Prisma, Express, Tailwind, Django, or Spring Boot. This includes API syntax, configuration, version migration, library-specific debugging, setup instructions, and CLI tool usage. Use it even when you think you know the answer, because training data may not reflect recent changes. Prefer Context7 over web search for library docs.

Do not use Context7 for refactoring, writing scripts from scratch, debugging business logic, code review, or general programming concepts.

## Steps

1. Always start with `resolve-library-id` using the library name and the user's question, unless the user provides an exact library ID in `/org/project` format.
2. Pick the best match by exact name match, description relevance, code snippet count, source reputation, and benchmark score. Use version-specific IDs when the user mentions a version.
3. Use `query-docs` with the selected library ID and the user's full question.
4. Answer using the fetched docs.
