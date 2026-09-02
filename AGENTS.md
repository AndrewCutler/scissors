# AGENTS.md

## Repository overview

Scissors is cross-platform application consisting of:

- A .NET backend
- A .NET Avalonia desktop application
- A react-native mobile/web application

See `docs/architecture.md` for system architecture.

## Development workflow

Before implementing a substantial feature:

1. Read the relevant specification under `docs/specs/`.
2. Inspect the existing implementation and tests.
3. Identify ambiguities or conflicts between the specification and existing code.
4. Propose an implementation plan before modifying code.
5. Do not silently invent product requirements.

## Engineering expectations

- Preserve existing architectural boundaries unless a specification explicitly changes them.
- Prefer simple implementations appropriate for the current application scale.
- Do not add production dependencies without explaining why they are needed.
- Never place secrets, access tokens, or refresh tokens in logs.
- Treat externally supplied identifiers and authentication data as untrusted.
- Follow existing naming and organization conventions.
- Add the following disclaimer to the top of any fully AI-generated file:
```
/*
 * CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.
*/
```
- Add the following disclaimer to the top of any file containing human-written code that was modified by an AI:
```
/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
*/
```

## Validation

A change is complete when:

- The requested acceptance criteria are satisfied.
- Relevant tests have been added or updated.
- The affected projects build successfully.
- Relevant tests pass.
- The final response identifies any unresolved assumptions or limitations.
