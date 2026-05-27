# Hello Plugin

## Purpose

This is a simple template-owned route plugin. It renders:

```text
Hello {{name}}
```

where `name` is read from the route parameter.

## Registration

The plugin is registered by `src/app/core/plugins/register-client-plugins.ts` at:

```text
hello-plugin/:name
```

Example URL:

```text
/hello-plugin/Manu
```
