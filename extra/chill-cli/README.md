# Chill CLI

Create a ChillSharp workspace that is ready for your preferred coding agent.

```bash
npm install -g @chill-sharp/chill-cli
chill new my-data-project
cd my-data-project
```

`chill new` creates the project directory and installs the maintained ChillSharp agent skills in `.agents/skills`. It also adds `AGENTS.md` with instructions that agent tools can follow while developing the project.

For a normal application request, that guidance requires the coding agent to deliver a connected system:

- an ASP.NET Core backend using ChillSharp and EF Core;
- an authenticated management UI that changes real backend data; and
- a user-facing frontend that reads and writes the same API.

The agent should only omit one of those layers when the request explicitly asks for an API-only, UI-only, or static prototype. A visual-only frontend, mock data, and browser-only persistence do not satisfy a full application request.

Use a concrete prompt that names the user roles and first workflow. For example:

> Create a blog with an ASP.NET Core ChillSharp backend and middleware. Authors must be able to sign in and manage posts and categories in an admin UI. Visitors must be able to browse published posts in a separate frontend. Both UIs must use the same API and database.

The command uses only Node.js standard-library APIs, so it works on Linux, macOS, and Windows with Node.js 20 or later.

## Command

```text
chill new <project-name>
```

Project names use lowercase letters, digits, and hyphens. The command refuses to overwrite an existing directory.
