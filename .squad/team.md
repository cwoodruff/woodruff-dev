# Squad Team

> woodruff-dev

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Mal | Lead / Architect | `.squad/agents/mal/charter.md` | ✅ Active |
| Kaylee | Astro Frontend Engineer | `.squad/agents/kaylee/charter.md` | ✅ Active |
| Wash | Build & Deploy Engineer | `.squad/agents/wash/charter.md` | ✅ Active |
| Inara | Content & SEO Specialist | `.squad/agents/inara/charter.md` | ✅ Active |
| Simon | Test Engineer | `.squad/agents/simon/charter.md` | ✅ Active |


## Built-in Support Agents

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Scribe | Decision Merger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | 🔄 Monitor |
| Rai | RAI Reviewer | `.squad/agents/rai/charter.md` | 🛡️ RAI |
| Fact Checker | Devil's Advocate & Verification Agent | `.squad/agents/fact-checker/charter.md` | 🔍 Verifier |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Project:** woodruff-dev
- **Created:** 2026-11-25
- **Stack:** Astro 7 static site in `site/` (Node >= 22.12), pagefind search, RSS + sitemap, Substack essay sync script.
- **Purpose (assumed):** woodruff.dev personal site and blog.
- **Casting universe:** Firefly.
- **Reviewers:** Mal (code/architecture) and Simon (tests/build verification); reviewer is never the author.
