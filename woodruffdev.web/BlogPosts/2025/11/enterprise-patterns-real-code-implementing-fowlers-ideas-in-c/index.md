---
title: "Enterprise Patterns, Real Code: Implementing Fowler’s Ideas in C#"
date: 2025-11-28
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "Enterprise-Patterns-Real-Code-Implementing-Fowlers-Ideas-in-C.png"
---

Most enterprise systems already use patterns from [Martin Fowler’s _Patterns of Enterprise Application Architecture_](https://martinfowler.com/eaaCatalog/). The twist is that many teams use them without naming them, half implement them and then wonder why the codebase fights back.

If your ASP.NET solution contains controllers that talk straight to SQL, services that return HTTP responses, and entities that call SaveChanges, you are already remixing Fowler’s patterns. You are just doing it implicitly and at great expense.

This series takes the opposite route. We will name the patterns, show where they fit, and implement each one in C# with concrete examples.

You will see where these patterns help, where they hurt, and how they combine into real architectures instead of diagram fantasies.

## **How this series will work**

In each article, I will:

- - Explain a pattern in plain language, with the original intent from Fowler’s catalog

- - Show how it typically shows up in C# and .NET projects

- - Give a focused example of when to use it and when to avoid it

- - Connect it to neighboring patterns so you see design options, not isolated tricks

This introduction maps the territory. Every item in the index below links to a short description, a definition, and an example scenario. Each of those sections will become a full post with code.

## **Pattern index**

Use these links to jump to the pattern you care about right now.

- - **[Layered Architecture](https://www.woodruff.dev/stop-letting-your-controllers-talk-to-sql-layered-architecture-in-asp-net-core/)**

- - **[Transaction Script](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-transaction-script-pattern-the-shortcut-that-quietly-reshapes-your-system/)**

- - **[Domain Model](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-domain-model-pattern-when-your-core-rules-deserve-their-own-gravity/)**

- - **[Service Layer](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-service-layer-pattern-making-http-a-client-not-the-boss/)**

- - **[Active Record](https://woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-active-record-pattern/)**

- - **[Data Mapper](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-data-mapper-pattern/)**

- - **[Repository](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-repository-pattern/)**

- - **[Unit of Work](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-unit-of-work-pattern/)**

- - **[Identity Map](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-identity-map-pattern/)**

- - **[Lazy Load](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-minimal-api-lazy-load-pattern/)**

- - **[Front Controller and MVC](https://www.woodruff.dev/enterprise-patterns-for-asp-net-core-front-controller-and-mvc-pattern/)**

- - Data Transfer Object (DTO)

Each section below is both a preview and a contract for the dedicated C# article that will follow.

## Layered Architecture Pattern

#### **Definition**

Layered Architecture splits an application into distinct layers with clear responsibilities. Fowler’s baseline is:

- - Presentation: handles input and output

- - Domain: holds business rules and domain logic

- - Data source: manages persistence and integration with data stores

The core rule is brutal and simple: if your controllers know SQL or your repositories know HTTP, the layers have already collapsed.

#### **Where to use it**

Layered Architecture fits:

- - ASP.NET Core applications that will grow beyond a few controllers

- - Systems where different teams own UI, business rules, and infrastructure

- - Codebases that must survive several technology shifts over their lifetime

In the dedicated post you will see a C# solution where projects align with layers, controllers stay thin, domain services stay ignorant of transport, and repositories encapsulate EF Core without leaking it upward.

## Transaction Script Pattern

#### **Definition**

Transaction Script organizes business logic as procedures that handle a single request or use case end to end. Each script:

- - Reads input

- - Performs calculations and decisions

- - Persists changes

There is minimal domain modeling. The focus stays on the flow of a transaction.

#### **Where to use it**

Transaction Script works best when:

- - The domain logic is simple and shallow

- - You are building reports, admin utilities, or migration tools

- - You need results quickly and long term complexity is limited

In C#, this often appears as an application service or handler class that works directly with DbContext and simple DTOs. In the article you will see both the benefits and the trap: it feels efficient until rules start to repeat across scripts.

## Domain Model Pattern

#### **Definition**

Domain Model concentrates business rules inside a rich object model. Entities and value objects express invariants and behavior.

Instead of treating data as passive structures, you treat the domain as the center of gravity. Controllers and repositories orbit around it rather than injecting rules into every edge of the system.

#### **Where to use it**

Domain Model earns its weight when:

- - The business rules are complex and interdependent

- - Invariants matter more than raw throughput

- - You expect requirements to evolve frequently

In C# this means entities with methods that enforce rules, factories that control creation, and services that orchestrate multiple aggregates. The dedicated post will show an aggregate in code, along with tests that lock in behavior before you worry about EF mapping.

## Service Layer Pattern

#### Definition

Service Layer defines a set of application operations that sit between the outside world and the domain model. It:

- - Coordinates multiple domain objects

- - Handles transactions and security policies

- - Exposes a clear API for controllers, message handlers, or other clients

It is the point where use cases live.

#### **Where to use it**

Service Layer fits when:

- - You have multiple clients hitting the same core logic: web, background jobs, workers

- - You want to expose a stable application API while the UI evolves

- - Cross cutting concerns such as logging, permissions, and transaction boundaries must stay consistent

In .NET this often becomes a set of application service classes injected into controllers and workers. In the article you will see a C# service layer that makes HTTP a detail, not the boss.

## Active Record Pattern

#### **Definition**

Active Record merges domain objects with persistence. Each entity:

- - Maps directly to a database row

- - Contains business logic

- - Knows how to load and save itself

Fowler treats it as a close fit for simple domains where the object model mirrors the database closely.

#### **Where to use it**

Active Record suits:

- - Small systems with straightforward tables

- - Prototypes where getting something working matters more than deep abstraction

- - Places where simple CRUD with light behavior is enough

In C#, you often see Active Record flavor when EF Core entities call SaveChanges directly or static methods perform global queries. The dedicated post will show a disciplined version of Active Record and explain when to retire it in favor of a separate Data Mapper.

## Data Mapper Pattern

#### **Definition**

Data Mapper sits between domain objects and the database. It:

- - Loads domain objects from data stores

- - Persists changes back

- - Shields the domain from knowledge of how persistence works

The domain classes stay persistence ignorant. The mapper takes on the burden of translation.

#### **Where to use it**

Data Mapper pays off when:

- - You have a rich Domain Model

- - The database schema must evolve independently of the object model

- - You want to test domain logic without a database in the way

In .NET, EF Core already plays the Data Mapper role. The article will show how to design domain classes that do not depend on EF, then map them using configurations and repositories that wrap the mapper.

## Repository Pattern

#### **Definition**

Repository represents a collection-like interface for accessing aggregates. It:

- - Hides queries and persistence details

- - Exposes methods that work in domain terms, such as GetById, FindActiveForCustomer, Add, Remove

- - Lets the domain talk in its own language instead of in SQL or query APIs

Fowler includes Repository in the object relational patterns as a way to further isolate domain logic from data access.

#### **Where to use it**

Repository helps when:

- - The same aggregate appears across many use cases

- - You want consistent access patterns for aggregates

- - You expect to support multiple query strategies or stores behind the same domain interface

In C#, this usually means interface definitions in the domain layer and implementations in an infrastructure project. The dedicated post will include concrete repository designs, and also examples of where a repository introduces more indirection than it earns.

## Unit of Work Pattern

#### **Definition**

Unit of Work tracks changes to domain objects during a business transaction and writes them out as a single logical batch. It:

- - Records inserts, updates, and deletes

- - Coordinates commit or rollback

- - Provides a boundary for transactional behavior

Fowler presents it as a way to stop writes from spreading unpredictably through a codebase.

#### **Where to use it**

Unit of Work is valuable when:

- - A single operation touches multiple aggregates or tables

- - You need clear transactional boundaries for consistency

- - You want to keep domain logic free of save calls

In .NET, DbContext already behaves as a Unit of Work, yet many codebases hide that fact. The article will show how to embrace this pattern explicitly and how to wrap EF Core in a higher level unit of work abstraction when needed.

## Identity Map Pattern

#### **Definition**

Identity Map ensures that each logical entity from the database exists only once in memory per scope. It:

- - Tracks loaded objects by identity

- - Returns existing instances instead of creating new ones for the same key

- - Helps avoid inconsistent in memory states for the same row

This pattern often works with Unit of Work and Data Mapper.

#### **Where to use it**

Identity Map matters when:

- - The same entity is loaded through different paths in one request

- - You attach domain behavior to entities and depend on reference equality

- - You care about performance costs of repeated materialization

ORMs such as EF Core implement identity maps under the surface. The dedicated post will explain what EF is doing for you and show how to apply Identity Map explicitly when you move outside of ORMs or use multiple contexts.

## Lazy Load Pattern

#### **Definition**

Lazy Load defers loading of related data until it is actually needed. Instead of fetching entire object graphs, you:

- - Load a root entity

- - Represent associations as placeholders

- - Trigger actual loading when code accesses the association

The pattern targets performance and memory by avoiding unnecessary work.

#### **Where to use it**

Lazy Load helps when:

- - Most use cases do not need full graphs

- - Some navigations are expensive or remote

- - You have to control query explosions carefully

In .NET, EF Core can use lazy loading proxies, or you can code your own lazy associations. The article will show both approaches and highlight the risk: invisible queries that surprise you in performance profiles.

## Front Controller Pattern

#### **Definition**

Front Controller centralizes request handling for a web application. Instead of letting every page or endpoint own its own entry point, you:

- - Route all requests through a single handler

- - Apply cross cutting logic in one place

- - Delegate to controllers or handlers for detailed work

Fowler introduces it as a response to duplicated request handling logic.

#### **Where to use it**

Front Controller aligns with:

- - Web applications that need consistent logging, authentication, and error handling

- - Systems that must make routing decisions based on shared policies

- - Architectures that use pipelines and middleware

In ASP.NET Core, the combination of the hosting pipeline and routing already forms a Front Controller. The dedicated post will show how to control that pipeline intentionally instead of treating it as framework magic.

## Model View Controller (MVC) Pattern

#### **Definition**

Model View Controller splits UI logic into three parts:

- - Model: the underlying data and behavior

- - View: rendering logic

- - Controller: input handling and coordination

Fowler’s version focuses on server side web MVC.

#### **Where to use it**

MVC suits:

- - Applications with complex UI interactions that depend on domain rules

- - Teams that want clear separation between presentation logic and domain logic

- - Systems that must support multiple views on the same model

In ASP.NET Core MVC, controllers speak to application services, views render models, and domain rules stay out of both. The article will show how to keep controller code lean instead of letting it morph into a second application layer.

## Data Transfer Object (DTO) Pattern

#### **Definition**

Data Transfer Object carries data across process boundaries. It:

- - Aggregates fields into a serializable shape

- - Avoids sending full domain objects across the wire

- - Provides a contract between services, clients, or layers

DTOs trade object richness for stability and clarity at integration points.

#### **Where to use it**

DTOs are worth the effort when:

- - You are exposing public APIs

- - Multiple clients consume your service, each with their own evolution pace

- - You want to keep domain classes internal to your application

In C#, DTOs typically appear as record types in API projects or as message contracts in messaging systems. The article will show mapping patterns between domain objects and DTOs and how to keep them from overflowing with accidental complexity.

## What comes next

The rest of this series will go pattern by pattern:

- - Each pattern gets its own post

- - Each post includes C# examples, tests where relevant, and context from real projects

- - The focus stays on tradeoffs, not worship of diagrams

You can read the series start to finish, or you can drop directly into the pattern that matches the pain in your current system and work outward from there.

If your code already resembles these patterns, this series gives you language and structure. If it does not, the upcoming posts will show how to reshape it piece by piece without pausing delivery.
