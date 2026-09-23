# Chill CLI

Create a ChillSharp workspace that is ready for your preferred coding agent.

```bash
npm install -g @chill-sharp/chill-cli
chill new my-data-project
cd my-data-project
```

`chill new` creates the project directory and installs the maintained ChillSharp agent skills in `.agents/skills`. It also adds `AGENTS.md` with instructions that agent tools can follow while developing the project.

The command uses only Node.js standard-library APIs, so it works on Linux, macOS, and Windows with Node.js 20 or later.

## Command

```text
chill new <project-name>
```

Project names use lowercase letters, digits, and hyphens. The command refuses to overwrite an existing directory.
