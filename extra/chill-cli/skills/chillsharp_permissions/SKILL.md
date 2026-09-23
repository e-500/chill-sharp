---
name: chillsharp_permissions
description: Guidance on using, evaluating, and managing the authorization and permission model in ChillSharp.
---

# ChillSharp Permissions & Authorization

This skill explains how ChillSharp's default-deny, hierarchical permission model works and how to utilize the Auth API.

## 1. Subjects and Precedence

Permissions can be assigned to **Users** or **Roles**. The precedence of resolution is:
1. User property rules
2. User entity rules
3. User module rules
4. Role property rules
5. Role entity rules
6. Role module rules
7. Default Deny

## 2. Resource Hierarchy and Actions

The resource hierarchy is evaluated as:
`Module -> Entity -> Property`

- **Entity Actions**: `Query`, `Create`, `Update`, `Delete`.
- **Property Actions**: `See`, `Modify`.
  - Property rules refine an allowed entity operation; they do not replace them.
  - To edit a property, you need entity `Create`/`Update` and property `Modify`.
  - To see a property, you need entity `Query` and property `See`.

## 3. Configuration & Enforcement

- The system uses a **default-deny** posture. If no rule allows access, it is blocked.
- Define permission rules via `AuthPermissionRule` entries.
- Server-side validation automatically executes authorization rules during CRUD actions.
- Clients can fetch user permissions via `/api/chill-auth/get-permissions` and evaluate access rules locally.

## 4. API Endpoints

Privileged users with `CanManagePermissions` can manage ACL/auth settings via:
- `GET chill-auth/get-user-list`
- `GET chill-auth/get-user`
- `POST chill-auth/set-user` (updates roles & user-specific permissions incrementally)
- `GET chill-auth/get-role-list`
- `GET chill-auth/get-role`
- `POST chill-auth/set-role` (updates role permissions & user-role assignments incrementally)
