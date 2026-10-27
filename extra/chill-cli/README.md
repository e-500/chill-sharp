# Chill CLI

Create a ChillSharp workspace that is ready for your preferred coding agent.

```bash
npm install -g @chill-sharp/chill-cli
chill new my-data-project
cd my-data-project
```

`chill new` asks whether to prepare a folder with only coding agent skills, create an ASP.NET Core API, an Angular UI, or both. All choices create a project directory and install the maintained ChillSharp agent skills in `.agents/skills`. The API and UI choices also add `AGENTS.md` with instructions that agent tools can follow while developing the project.

When scaffolding an API or UI interactively, `chill new` asks for each project name. It suggests a PascalCase C# project name and a lowercase kebab-case Angular project name based on the workspace name; blank input accepts the suggestion. Entered names can use spaces, hyphens, PascalCase, or camelCase and are normalized for the target project. The Angular page title uses a readable title case version of the workspace name.

The API is copied from the repository's `ChillSharp.Template` project and restored from NuGet. The UI is copied from `chill-sharp-ui-template`; its ChillSharp Angular packages are installed from npm. No local NuGet packages or `.tgz` archives are copied into the generated workspace.

The API project is located at `api/ChillSharp.Template.csproj`, and the Angular project keeps the `chill-sharp-ui-template` project configuration. For an application request, the included guidance requires the coding agent to deliver a connected system:

- an ASP.NET Core backend using ChillSharp and EF Core;
- an authenticated UI that changes real backend data; and
- a separate user-facing frontend when the requested users or workflows need one.

The generated ASP.NET Core project already includes ChillSharp, a configured context, and mapped APIs. Extend its sample model and workflow for the application request. A visual-only frontend, mock data, and browser-only persistence do not satisfy a full application request.

Use a concrete prompt that names the user roles and first workflow. For example:

> Create a blog with an ASP.NET Core ChillSharp backend and middleware. Authors must be able to sign in and manage posts and categories in an admin UI. Visitors must be able to browse published posts in a separate frontend. Both UIs must use the same API and database.

The command uses Node.js standard-library APIs for scaffolding and package managers to restore dependencies, so it works on Linux, macOS, and Windows with Node.js 20 or later.

## Command

```text
chill new <project-name> [--skills|--api|--ui|--both]
```

Without an option, the command displays this menu:

```text
0. Prepare a folder with only coding agent skills
1. API (ASP.NET Core + ChillSharp)
2. UI (Angular + ChillSharp UI)
3. API and UI
```

Use `--skills`, `--api`, `--ui`, or `--both` for a non-interactive run. Option `0` and `--skills` create only `.agents/skills` inside the new folder; they do not scaffold a project or install packages. Project names use lowercase letters, digits, and hyphens. The command refuses to overwrite an existing directory. Creating an API requires the .NET SDK; creating a UI requires npm and internet access to the public package registries.

## Install a client package

From the target project directory, run:

```text
chill install
```

The menu can install one published dependency at a time:

- `@chill-sharp/ng-client`, `@chill-sharp/vue-client`, `@chill-sharp/react-client`, or `@chill-sharp/ts-client` with npm;
- `ChillSharp` or `ChillSharp.Client` with `dotnet add package`; or
- `chill-sharp-py-client` with `python -m pip install`.

Use a non-interactive option such as `chill install --vue-client`. For .NET packages, run the command from the directory containing the target `.csproj` file.
