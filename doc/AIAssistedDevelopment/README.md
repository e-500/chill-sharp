# AI-Assisted Development Guide

This document explains how ChillSharp can help when you build software with AI assistance and still want the codebase to remain structured, stable, and reviewable.

The key idea is simple: AI tools are much more reliable when they work inside a constrained, repetitive, well-defined architecture than when they are asked to manually keep many controllers, DTOs, endpoints, and validation paths in sync.

ChillSharp does not make AI-generated code automatically correct. What it does is reduce the amount of surface area that AI has to generate and maintain.

## Why This Matters

A common failure mode in AI-assisted backend development is that the model touches too many moving parts at once:

- controllers
- DTO mappings
- request/response contracts
- validation logic
- authorization checks
- duplicated CRUD behaviors

The more files and custom endpoints you have, the easier it is for AI to introduce accidental interface drift, inconsistent behavior, or broad refactors that were never intended.

ChillSharp reduces that risk by moving a large part of the backend surface into a uniform model-driven runtime.

## How ChillSharp Helps

### 1. Business logic grows inside a structured environment

With ChillSharp, the main extension points are explicit and predictable:

- `ChillEntity`
- `ChillQuery`
- `OnValidation()`
- lifecycle hooks such as `OnCreate()`, `OnUpdate()`, `OnAfterUpdate()`, `OnDelete()`, and `OnSelect()`
- metadata through `[ChillProperty]` and related annotations

That gives AI a narrower and more structured place to make changes.

Instead of asking an AI model to invent yet another controller, request DTO, response DTO, mapper, validator, and route contract, you can often ask it to:

- add a property
- add validation
- add a query filter
- add lifecycle logic
- adjust permission rules

This usually produces smaller and safer edits.

### 2. Lower risk of accidental endpoint refactoring

ChillSharp exposes a standard API surface through `app.MapChillApi()`, with stable operations such as:

- `POST /api/chill/query`
- `POST /api/chill/find`
- `POST /api/chill/create`
- `POST /api/chill/update`
- `POST /api/chill/delete`

Because the transport surface is centralized, adding or evolving business entities does not require AI to keep rewriting a growing set of per-entity controllers and route definitions.

This reduces a specific AI risk:

- changing endpoint names by accident
- changing payload shapes inconsistently
- implementing one endpoint differently from the rest
- breaking clients through unnecessary API refactors

The interface still evolves when your model evolves, but the CRUD and query mechanics do not have to be re-authored every time.

### 3. Endpoints grow in a uniform way

In a traditional hand-written backend, every new entity tends to create more duplicated API code. Over time, small differences accumulate:

- one controller validates differently
- another controller returns slightly different payloads
- another endpoint forgets an authorization check
- another DTO mapper omits a field

AI tools amplify this problem because they continue the local pattern they see, even when the local pattern is already inconsistent.

ChillSharp pushes the system in the opposite direction: entities and queries plug into the same runtime model, so growth is more uniform by default.

That uniformity helps both:

- human maintainers reviewing AI-produced changes
- AI tools reasoning over the codebase with less ambiguity

### 4. Smaller program payload for AI tools

When a project relies on many custom CRUD controllers, DTO classes, mapping layers, and repetitive endpoint definitions, AI needs more repository context to make a safe change.

That increases:

- token usage
- latency
- cost
- the chance that the model misses one of the duplicated layers

ChillSharp reduces this burden because much of the repetitive transport logic is already handled by the framework runtime.

In practice this means an AI task can often be solved by reading and changing:

- one entity
- one query
- one validation rule
- one permission definition

instead of a long chain of related files.

### 5. Lower pressure for continuous large-scale refactoring

Without a model-driven framework, teams often ask AI to keep refactoring a growing list of:

- endpoints
- controllers
- DTOs
- validators
- mappers
- permission checks

That is expensive and fragile. It also encourages broad automated rewrites that may not deliver business value.

ChillSharp reduces the need for that style of maintenance because the generic CRUD/query surface is already centralized.

That has practical benefits:

- lower AI token consumption
- fewer broad refactors across repetitive files
- less review effort for generated code
- lower compute usage for the same feature work

If you care about both engineering efficiency and energy efficiency, this is one of the strongest arguments for using a uniform runtime instead of a large amount of repeated endpoint boilerplate.

## What ChillSharp Is Good At In AI Workflows

ChillSharp is a good fit when you want AI to help with:

- extending domain entities
- adding validation rules
- adding query capabilities
- exposing model changes through an existing generic API surface
- keeping permissions and metadata closer to the model

This is usually a better fit than asking AI to repeatedly generate large sets of CRUD infrastructure code.

## What ChillSharp Does Not Solve

ChillSharp does not remove the need for engineering review. In particular, you still need to verify:

- business rules are correct
- authorization rules are correct
- exposed properties are intentional
- model changes do not break consumers
- AI-generated lifecycle logic is actually safe

ChillSharp reduces duplication and drift. It does not remove the need for judgment.

## Recommended Positioning

If you want a short and defensible way to describe this in documentation or architecture notes, use something like:

"ChillSharp helps AI-assisted development by centralizing repetitive API mechanics into a model-driven runtime. This reduces accidental interface drift, keeps endpoint behavior more uniform, and lowers the amount of code and repository context that AI tools must generate and maintain."

## Practical Checklist

If you want to use ChillSharp as an AI-friendly backend architecture, the baseline is:

1. keep business entities and queries as the main place where feature behavior is defined
2. use `[ChillProperty]` consistently so the DTO and validation surface remains intentional
3. prefer `OnValidation()` and lifecycle hooks over ad-hoc controller logic
4. avoid reintroducing repetitive custom CRUD endpoints unless there is a real need
5. review model changes carefully because a model-driven surface can affect multiple client operations at once
6. keep permissions and authentication aligned with the same model-driven approach

## Related Documents

- [../README.md](../README.md)
- [../RegisterContext.md](../RegisterContext.md)
- [../ValidationModel/README.md](../ValidationModel/README.md)
- [../PermissionModel/README.md](../PermissionModel/README.md)
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)
- [../ClientGeneration/README.md](../ClientGeneration/README.md)
