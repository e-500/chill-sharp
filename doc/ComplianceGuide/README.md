# Security And Compliance Guide

This document explains how ChillSharp can support security and compliance programs such as NIS2, ISO 27001, SOC 2, or internal secure-development policies.

It is intentionally generic: compliance frameworks differ by jurisdiction and sector, but many of the underlying engineering controls are the same.

Important: ChillSharp can help you implement and enforce several technical controls automatically, but using ChillSharp does not by itself make an application compliant. Compliance still depends on your hosting environment, operating procedures, monitoring, incident response, backup strategy, and legal scope.

## Where ChillSharp Helps

ChillSharp is useful when you want your API layer to apply the same validation, authorization, metadata, and audit conventions consistently across the whole model instead of re-implementing them controller by controller.

That consistency matters in compliance work because many findings come from gaps between endpoints, forgotten checks in one update path, or UI and API behavior drifting apart over time.

## Control Areas Supported By ChillSharp

### 1. Input validation and data integrity

ChillSharp helps reduce invalid or unsafe data entering the system by centralizing validation around the entity and query model.

- standard DataAnnotations such as `[Required]`, `[StringLength]`, `[Range]`, and `[EmailAddress]` can be applied on `[ChillProperty]` members
- the validation pipeline runs during explicit `VALIDATE()` flows
- the same validation also runs automatically when the client goes directly to create or update
- custom business validation can be added through `OnValidation()`

This supports control objectives typically described as:

- input validation
- data quality enforcement
- secure-by-default server-side validation
- reduction of inconsistent validation across endpoints

Reference:
- [../ValidationModel/README.md](../ValidationModel/README.md)

### 2. Authentication and controlled access

With `ChillSharp.Auth`, the host can expose Identity-backed account flows for:

- registration
- login
- refresh-token handling
- password change
- password reset

This helps standardize the access layer and avoid ad-hoc authentication endpoints with inconsistent behavior.

Reference:
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)

### 3. Authorization and least privilege

ChillSharp provides a default-deny permission model with:

- user permissions
- role permissions
- module, entity, and property scopes
- allow/deny rules
- explicit precedence rules

This is useful for compliance programs that expect least-privilege access, separation of duties, and clear server-side enforcement of who can see or modify data.

Because property-level permissions are part of the model, ChillSharp can help reduce a common risk: users having access to the right entity but too much access to sensitive fields.

Reference:
- [../PermissionModel/README.md](../PermissionModel/README.md)

### 4. Audit trail fields on data changes

`ChillEntity` automatically maintains:

- `LastUpdate`
- `LastUpdateUser`
- `LastUpdateUtcOffset`
- `Checksum`

These values are updated as part of the runtime path used by ChillSharp during updates, which helps enforce a consistent minimum audit trail without depending on every derived entity to remember to do it manually.

This supports common control objectives such as:

- traceability of changes
- accountability of user actions
- basic integrity checking
- evidence that records were modified, when, and by whom

The checksum is especially useful as a lightweight integrity signal for synchronization, comparisons, and tamper detection scenarios inside the application model.

Reference:
- [../README.md](../README.md#audit-fields)

### 5. Consistent schema metadata and safer client generation

ChillSharp can expose schema metadata and generate clients from the API description.

This does not replace a security control by itself, but it can reduce implementation drift between:

- backend validation and frontend forms
- backend authorization and frontend capabilities
- actual API contracts and hand-written clients

Reducing drift matters in audits because inconsistent clients and duplicated API glue often create hidden exceptions to the intended control model.

Reference:
- [../ClientGeneration/README.md](../ClientGeneration/README.md)

## Why This Matters For NIS2 And Similar Frameworks

Frameworks such as NIS2 usually do not certify a library. They expect organizations to implement risk-based technical and organizational measures.

In that context, ChillSharp is best understood as a control-enforcement component that can help with:

- identity and access control
- least privilege
- traceability of updates
- consistent validation of incoming data
- reduction of manual security plumbing

This can lower the probability of common implementation defects and make the application easier to review during internal audits or external assessments.

## What ChillSharp Does Not Solve On Its Own

You still need to design and operate the broader security system around the library. In particular, ChillSharp does not by itself provide:

- a full SIEM or centralized security logging strategy
- incident detection and response procedures
- vulnerability management and patch governance
- infrastructure hardening
- network segmentation
- transport security configuration
- encryption key management
- secrets management
- backup and disaster recovery processes
- MFA policy and corporate identity governance
- supplier risk management
- legal interpretation of NIS2 or any other regulation

Those controls belong partly to your application, but mostly to your platform and organizational processes.

## Recommended Positioning In Audit Documentation

When documenting ChillSharp in a security review, describe it as:

- a framework that centralizes API validation
- a framework that enforces role- and property-based authorization
- a framework that maintains basic audit metadata on entity updates
- a framework that reduces inconsistent custom CRUD code

Avoid stronger claims such as:

- "the application is NIS2 compliant because it uses ChillSharp"
- "ChillSharp guarantees regulatory compliance"

The stronger and more defensible statement is:

"ChillSharp helps implement and automate several technical controls that are commonly required by security and compliance frameworks, while final compliance depends on the full system design and operating model."

## Practical Checklist

If you want to use ChillSharp as part of a compliance-oriented architecture, the baseline is:

1. use `ChillEntity` and annotate exposed properties with `[ChillProperty]`
2. add DataAnnotations and custom `OnValidation()` rules for business constraints
3. enable `ChillSharp.Auth` for authenticated systems
4. configure roles and permission rules with default-deny posture
5. verify that `GetCurrentUserName()` is correctly implemented in your `IChillContext`
6. preserve and monitor `LastUpdate`, `LastUpdateUtcOffset`, `LastUpdateUser`, and `Checksum`
7. secure the host with HTTPS, logging, backups, patching, and operational controls outside ChillSharp

## Related Documents

- [../ValidationModel/README.md](../ValidationModel/README.md)
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)
- [../PermissionModel/README.md](../PermissionModel/README.md)
- [../ClientGeneration/README.md](../ClientGeneration/README.md)
- [../RegisterContext.md](../RegisterContext.md)
