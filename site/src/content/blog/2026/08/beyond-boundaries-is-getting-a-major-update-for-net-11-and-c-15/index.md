---
title: "Beyond Boundaries Is Getting a Major Update for .NET 11 and C# 15"
date: 2026-08-21
description: "When I first published Beyond Boundaries: Networking Programming with C# and .NET, I knew it would need to evolve. Networking doesn’t stand still, and neither does the .NET platform. What I didn’t…"
categories:
  - "network-programming"
tags:
  - "net"
  - "c"
  - "dotnet"
  - "network"
  - "programming"
coverImage: "./images/beyond-boundaries-is-getting-a-major-update-for-net-11-and-c-15.png"
---
When I first published *Beyond Boundaries: Networking Programming with C# and .NET*, I knew it would need to evolve. Networking doesn’t stand still, and neither does the .NET platform. What I didn’t expect was just how much ground .NET 11 and C# 15 would cover. Enough to warrant the largest update the book has ever received.

I’m excited to share that **the updated edition is in active development**, targeting the .NET 11 GA release this November. The book is growing from 17 to 19 chapters, with over 200 pages of new content. Here’s what’s coming.

# Two Brand-New Chapters

## Chapter 7: Network Security and Modern Authentication

Security has always been threaded through the protocol-specific chapters, but readers have consistently asked for a unified treatment. This new chapter delivers it:

-   **TLS 1.3 configuration and observability** – including .NET 11’s new ITlsHandshakeFeature.Exception for diagnosing handshake failures and UseTlsClientHelloListener for ClientHello inspection
-   **Post-quantum cryptography preparation** – NIST standards (ML-KEM, ML-DSA, SLH-DSA), cryptographic agility patterns, and hybrid key exchange using .NET 11’s new X25519DiffieHellman class
-   **Passkeys and WebAuthn** – complete registration and authentication flows with the FIDO2 library, plus .NET 11’s AAGUID-based display name inference for a better user experience
-   **OAuth 2.0 and OpenID Connect** – authorization code with PKCE, JWT bearer authentication, token refresh strategies
-   **API security patterns** – API key management, HMAC request signing, rate limiting, security headers, and .NET 11’s default CSRF protection
-   **Zero trust networking** – with .NET 11’s cross-stack IAuthorizationRequirementData support across REST endpoints, SignalR hubs, and Blazor components

## Chapter 19: Building Cloud-Native Applications with .NET Aspire

This capstone chapter brings together concepts from throughout the book:

-   **Service orchestration** – defining distributed applications with the AppHost, service discovery, dependency management
-   **Built-in observability with OpenTelemetry** – updated for .NET 11’s native ASP.NET Core tracing (no more OpenTelemetry.Instrumentation.AspNetCore package required)
-   **Health checks and readiness probes** – liveness, readiness, and startup semantics with Aspire’s automatic integration
-   **Aspire components** – databases, caching, and messaging walk-throughs with a complete e-commerce example
-   **Testing distributed applications** – DistributedApplicationTestingBuilder with real dependencies
-   **Deployment patterns** – Azure Container Apps, Kubernetes, and .NET 11’s multi-architecture container publishing

# Eight Updated Existing Chapters

The update touches chapters across the entire book. The headline changes:

**Chapter 4: Asynchronous Programming** gets the single most significant update – a new section on .NET 11’s **Runtime Async**, which moves async state machines from the compiler into the runtime. Cleaner stack traces (13 frames become 5), better debuggability, lower overhead, and JIT-level optimizations including factory intrinsics and ExecutionContext capture elimination. If you write async network code, and if you’re reading this book, you do; this is the section to watch.

**Chapter 6** adds **Polly v8 resilience patterns** with the new ResiliencePipeline API and Microsoft.Extensions.Http.Resilience integration.

**Chapter 8** adds **.NET 11 System.Text.Json improvements** (NDJSON streaming over PipeWriter, PascalCase naming) and a new section on **modeling protocol messages with C# 15 union types and closed hierarchies**.

**Chapter 9** adds **Native AOT compilation** for network applications, **zero-copy stream types**, and improvements to Kestrel’s non-throwing HTTP/1.1 parser.

**Chapter 10** adds **HttpClientFactory best practices**, **rate limiting middleware**, **Minimal APIs** with .NET 11’s \[ShortCircuit\] and async validation, and **OpenAPI 3.2**.

**Chapter 14** adds **gRPC JSON transcoding** for dual-protocol services.

**Chapter 17** adds SignalR’s **authentication refresh without reconnection**. A real-world pain point is finally resolved, along with **client cancellation of hub invocations**.

**Chapter 18** adds **HTTP/3 stream prioritization** via QuicStream.Priority and Kestrel’s improved first-request latency. I’m also recommending retitling from “Looking to the Future with QUIC” to shipping, tunable APIs.

# C# 15 Throughout

Beyond the dedicated union types section, every code sample across the book is being modernized for C# 15, with collection expression arguments, labeled break/continue in packet-processing loops, and all project files targeting net11.0.

# When Can You Get It?

I’m drafting against the .NET 11 previews now. The plan is to complete all new content and revisions alongside the .NET 11 GA release this November, with a full verification pass against the final bits. The updated edition will be available on [LeanPub](https://leanpub.com/csharp-networking). If you’ve already purchased the book there, you’ll get the update automatically.

The companion source code on [GitHub](https://github.com/cwoodruff/book-network-programming-csharp) will be updated with net11.0 sample projects as chapters are completed.

# What’s Deliberately Out of Scope

Two things I’m intentionally leaving out: the MCP server template in the .NET 11 SDK (AI/LLM-specific tooling is out of scope for a general-purpose networking book) and Blazor-specific features (UI-framework concerns rather than networking).

# Stay Updated

I’ll be posting progress updates as chapters are completed. If you have topics you’d like to see covered, find a bug in the current edition, or just want to say hello, reach out at [cwoodruff@live.com](mailto:cwoodruff@live.com) or open an issue on the [GitHub repo](https://github.com/cwoodruff/book-network-programming-csharp/issues).

The networking landscape keeps evolving, and so does this book. I can’t wait to share the updated edition with you.
