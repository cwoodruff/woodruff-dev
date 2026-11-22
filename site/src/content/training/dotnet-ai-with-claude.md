---
title: ".NET AI with Claude"
short: "Two days for .NET developers: use Claude Code as your pair programmer, then build real AI features into your own applications with the Anthropic C# SDK."
format: workshop
duration: "2 days"
level: intermediate
audience: ".NET developers who want to build real AI capabilities into their applications, and use AI to build them faster."
prerequisites:
  - "Comfortable with C# and ASP.NET Core"
  - "A laptop with the .NET SDK and Docker installed"
  - "Familiarity with Git and GitHub"
outcomes:
  - "Work effectively with Claude Code: project memory with CLAUDE.md, rules, custom agents, hooks, skills, and plugins"
  - "Connect MCP servers and drive spec-driven feature development"
  - "Call Claude from .NET with the Anthropic C# SDK, including streaming and prompt caching"
  - "Build tool use and agent loops, and ground answers with retrieval-augmented generation"
  - "Write your own MCP server in C#"
  - "Test AI systems and apply responsible AI practices: prompt-injection defenses, auditing, and token budgets"
build: "BookTracker, a realistic ASP.NET Core Minimal API for books, authors, reviews, and reading progress. Eleven buildable checkpoints take it from a starter app to an AI-powered service with chat, an agent, RAG recommendations, and its own MCP server."
modules:
  - { title: "Day 1: Installing Claude Code and creating project memory" }
  - { title: "Day 1: Steering Claude: rules, agents, hooks, skills, and plugins" }
  - { title: "Day 1: MCP integration and finding planted issues in a real codebase" }
  - { title: "Day 1: Building complete features from written specs" }
  - { title: "Day 2: The Anthropic C# SDK and your first chat endpoint" }
  - { title: "Day 2: Streaming, tool use, and the agent loop" }
  - { title: "Day 2: Retrieval-augmented generation with a vector store" }
  - { title: "Day 2: Building an MCP server in C#" }
  - { title: "Day 2: Testing AI systems and responsible AI" }
tech: ["Claude Code", "Anthropic C# SDK", "MCP", "RAG"]
image: "./images/dotnet-ai-with-claude.svg"
accent: "#C2410C"
site: "https://dotnetclaude.com/"
repo: "https://github.com/cwoodruff/dotnet-claude-workshop-content"
draft: false
order: 3
---

Day one is about using AI to build faster: Claude Code as a pair programmer that knows your codebase, follows your rules, and can be extended with your own agents, hooks, and skills. Day two is about building AI into what you ship: chat, streaming, tools, agents, RAG, and MCP, all in C#.

You don't just watch. Every module ends in a lab where you extend the app yourself, and every lab has a complete, independently buildable checkpoint, so you can skip ahead or catch up without git branch juggling. The workshop guide is shared with attendees on the day.
