# Modular Authorization and Permission Model

## Overview

This document describes a flexible authorization model designed for applications that require fine-grained access control. The model supports permissions at three hierarchical levels:

1. **Module**
2. **Entity**
3. **Property**

Permissions can be assigned either **directly to users** or **through roles**, with deterministic precedence rules ensuring consistent evaluation.

The system allows defining what actions a user **can** or **cannot** perform on resources such as database entities and their individual properties.

The primary goals of this model are:

* Flexibility
* Clear hierarchy
* Deterministic evaluation
* Scalability for complex systems
* UI support for hiding unauthorized actions
* Strong server-side enforcement

---

# Core Concepts

## Subjects

Permissions apply to **subjects**, which can be:

* **User** – a specific individual account.
* **Role** – a group of users that share common permissions.

A user may belong to **multiple roles**.

When permissions are evaluated, **user permissions take precedence over role permissions**.

---

# Resource Hierarchy

Resources are organized in three levels of specificity.

```
Module → Entity → Property
```

Each level refines the scope of the permission.

---

## Module

A **module** represents a functional area of the system.

Examples:

* `Accounting`
* `Accounting.General`
* `Accounting.Payroll`
* `Sales`
* `General`

Modules may contain **hierarchical names separated by dots**.
This allows grouping related entities under a logical structure.

Example module hierarchy:

```
Accounting
Accounting.General
Accounting.Payroll
Accounting.Tax
```

A permission assigned at the module level applies to **all entities within that module**, unless overridden by more specific rules.

---

## Entity

An **entity** represents a logical data structure or table within a module.

Examples:

* `Account`
* `Invoice`
* `Customer`
* `Employee`

Entities **must not contain dots**.

Entities always belong to exactly one module.

Example resource:

```
Module: Accounting.General
Entity: Account
```

---

## Property

A **property** represents a specific field or column within an entity.

Examples:

* `CompanyName`
* `CreditLimit`
* `InternalNotes`
* `Salary`

Properties **must not contain dots**.

Example resource:

```
Accounting.General.Account.CompanyName
```

---

# Permission Scope

Permissions may apply at one of three scopes:

| Scope    | Applies To                        |
| -------- | --------------------------------- |
| Module   | Entire functional module          |
| Entity   | A specific entity                 |
| Property | A specific field within an entity |

Each deeper scope represents **greater specificity**.

---

# Actions

Different actions apply depending on the scope.

## Entity Actions

Entity actions control operations performed on entire records.

Available actions:

| Action | Description                   |
| ------ | ----------------------------- |
| QUERY  | Retrieve data from the entity |
| CREATE | Create new records            |
| UPDATE | Modify existing records       |
| DELETE | Remove records                |

These correspond conceptually to CRUD operations.

---

## Property Actions

Property actions control visibility and modification of individual fields.

| Action | Description                                  |
| ------ | -------------------------------------------- |
| SEE    | Allows a property to be visible in responses |
| MODIFY | Allows a property to be changed              |

These actions only apply when entity-level permissions already allow the operation.

---

# Permission Effects

Each permission has an **effect**:

| Effect | Meaning                       |
| ------ | ----------------------------- |
| Allow  | Grants the specified action   |
| Deny   | Explicitly forbids the action |

Deny rules always override allow rules at the same level.

---

# Permission Assignment

Permissions may be assigned to:

* A **user**
* A **role**

A user may inherit permissions from multiple roles.

Example:

```
User: Alice
Roles: Manager, Auditor
```

Alice receives permissions from both roles.

---

# Permission Specificity

Permissions can exist at three levels of specificity:

1. **Module-level**
2. **Entity-level**
3. **Property-level**

Example permissions:

```
ALLOW QUERY Accounting.General
ALLOW UPDATE Accounting.General.Account
DENY MODIFY Accounting.General.Account.CreditLimit
ALLOW SEE Accounting.General.Account.CompanyName
```

---

# Permission Evaluation Order

When evaluating permissions, the system resolves rules in the following order:

### 1. Subject Priority

User permissions override role permissions.

```
User rules → Role rules
```

---

### 2. Specificity Priority

More specific permissions override broader ones.

Order of evaluation:

```
Property → Entity → Module
```

---

### 3. Effect Priority

Within the same specificity level:

```
Deny overrides Allow
```

---

# Final Resolution Order

Combining subject priority and specificity results in the following evaluation sequence:

1. User Property Permissions
2. User Entity Permissions
3. User Module Permissions
4. Role Property Permissions
5. Role Entity Permissions
6. Role Module Permissions
7. Default Deny

If no rule explicitly grants access, the system denies the action.

---

# Permission Evaluation by Operation

## Query Operations

To retrieve entity data:

1. The user must have **QUERY permission on the entity**.
2. Each property returned must also have **SEE permission**.

If a property lacks SEE permission, it must be:

* removed from the response, or
* returned as null or masked.

---

## Update Operations

To modify an entity:

1. The user must have **UPDATE permission on the entity**.
2. Each property being changed must have **MODIFY permission**.

If the user attempts to modify a property without MODIFY permission, the operation must be rejected or that field ignored.

---

## Create Operations

To create a new entity:

1. The user must have **CREATE permission on the entity**.
2. Each provided property must have **MODIFY permission**.

Setting a property during creation is treated as modifying that property.

---

## Delete Operations

To delete a record:

1. The user must have **DELETE permission on the entity**.

Property permissions are irrelevant for delete operations.

---

# Module-Level Permissions

Module permissions provide a way to grant broad access.

Example:

```
ALLOW QUERY Accounting
```

This allows querying all entities within the `Accounting` module and its submodules.

Example covered entities:

```
Accounting.General.Account
Accounting.Payroll.Employee
Accounting.Tax.Invoice
```

More specific entity or property permissions may override this access.

---

# Property Overrides

Property permissions may override broader entity permissions.

Example:

```
ALLOW UPDATE Accounting.General.Account
DENY MODIFY Accounting.General.Account.CreditLimit
```

Result:

Users may update the account entity but **cannot modify the CreditLimit field**.

---

# Wildcard Property Permissions

For convenience, property permissions may apply to all properties of an entity.

Example:

```
ALLOW SEE Accounting.General.Account.*
```

This grants visibility to all properties.

Specific properties can still be restricted:

```
DENY SEE Accounting.General.Account.InternalNotes
```

---

# Default Security Model

The system follows a **default deny** principle.

If no rule explicitly grants permission, access is denied.

This approach ensures that newly added entities or properties are not automatically exposed.

---

# Intended Usage

This permission model supports:

* Backend authorization enforcement
* UI capability filtering
* Role-based access control
* Field-level security
* Multi-module enterprise systems

The model allows building systems where permissions can control:

* which modules users access
* which entities they can manipulate
* which fields they can view or modify

---

# Summary

This authorization system provides a structured and extensible permission model built on:

* **Modules**
* **Entities**
* **Properties**
* **User and Role inheritance**
* **Allow and Deny rules**
* **Deterministic evaluation**

The hierarchy and precedence rules ensure that access control remains predictable even in complex scenarios involving multiple roles, overrides, and field-level restrictions.

The result is a scalable authorization framework suitable for applications that require both **coarse and fine-grained permission control**.
