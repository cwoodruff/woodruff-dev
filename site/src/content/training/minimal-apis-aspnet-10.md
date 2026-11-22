---
title: "Building Secure and Scalable Minimal APIs with ASP.NET 10"
short: "A full day building Roastery, a complete ordering API for a small coffee roaster, from an empty project to a secured, tested container running under .NET Aspire."
format: workshop
duration: "1 day"
level: intermediate
audience: "Developers new to Minimal APIs, and those who have built them since .NET 6 and want to sharpen their security and scalability skills."
prerequisites:
  - "Working knowledge of C# and ASP.NET Core"
  - "A laptop with the .NET 10 SDK, SQLite, and Postman installed"
  - "About fifteen minutes of pre-work to verify your environment"
outcomes:
  - "Design RESTful endpoints with Minimal API routing, endpoint groups, and dependency injection"
  - "Model a domain and build full CRUD with EF Core and SQLite"
  - "Secure endpoints with JWT bearer tokens and role-, claim-, and policy-based authorization"
  - "Apply HTTPS enforcement, secure headers, rate limiting, and OWASP mitigations"
  - "Add output caching, resilience, integration tests, and OpenAPI tooling"
  - "Run the API in a container under the .NET Aspire dashboard"
build: "Roastery: a menu anyone can browse, orders for signed-in customers, and a fulfillment side where baristas advance orders from Pending to PickedUp. By 5 PM it is persisted, secured, cached, resilient to a flaky payment provider, tested, and running in Aspire."
modules:
  - { title: "Welcome, environment check, and the Roastery tour", length: "20 min" }
  - { title: "Fundamentals and the endpoint pipeline", length: "70 min" }
  - { title: "Data, validation, and the shape of a good API", length: "75 min" }
  - { title: "Identity, tokens, and authorization", length: "75 min" }
  - { title: "Performance, resilience, and testing", length: "75 min" }
  - { title: "Aspire, containers, and shipping", length: "55 min" }
  - { title: "Wrap-up, retrospective, and where to go next", length: "20 min" }
tech: ["Minimal APIs", "EF Core", "JWT", ".NET Aspire"]
image: "./images/minimal-apis-aspnet-10.svg"
accent: "#92400E"
site: "https://minimalapiworkshop.com/"
repo: "https://github.com/cwoodruff/aspnet-minimal-api-workshop"
draft: false
order: 2
---

This is a workshop, not a lecture. Each module is a short instructor-led concepts segment, where the feature is built live with the decisions explained, followed by a lab where you implement it yourself in your own copy of Roastery.

Every lab ends at a git checkpoint tag, so if you fall behind you can always catch up and keep building with the room. You leave with a working codebase and patterns you can reuse on Monday morning.
