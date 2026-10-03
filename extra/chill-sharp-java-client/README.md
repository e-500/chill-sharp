# chill-sharp-java-client

Generic Java client for the ChillSharp HTTP API. It uses Java's built-in `HttpClient` and Jackson `JsonNode` payloads, so it can work with application-specific ChillSharp entities without generated Java models.

The client supports the standard `/api/chill`, `/api/chill-schema`, `/api/chill-auth`, and `/api/chill-i18n` endpoints. It targets Java 17 or later.

## Build

```bash
cd extra/chill-sharp-java-client
mvn package
```

## Quick start

Add the package to your Maven project after publishing it to your Maven repository, or use it as a local Maven module. Then:

```java
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import io.chillsharp.client.ChillSharpClient;

ObjectMapper mapper = new ObjectMapper();
ChillSharpClient client = new ChillSharpClient("http://localhost:5000", null, "it-IT");

ObjectNode post = mapper.createObjectNode();
post.put("ChillType", "Model.Post");
post.put("Guid", "00000000-0000-0000-0000-000000000001");
post.putObject("Properties").put("Title", "Hello").put("Author", "Ada Lovelace");

JsonNode created = client.create(post);
JsonNode found = client.find(created);
```

Pass a base server URL, an existing `/api/chill` URL, or another recognized ChillSharp endpoint URL. By default the client appends `/api/chill` to a server URL. To use a custom API base path or HTTP configuration, use the full constructor:

```java
import com.fasterxml.jackson.databind.ObjectMapper;
import java.net.http.HttpClient;
import java.time.Duration;

HttpClient http = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(10)).build();
ChillSharpClient client = new ChillSharpClient(
    "https://example.local", "custom-api", "your-jwt-token", "en-GB", http, new ObjectMapper());
```

## Operations

Core methods include `query`, `lookup`, `find`, `create`, `update`, `delete`, `autocomplete`, `validate`, and `chunk`. Schema methods include `getSchema`, `getSchemaList`, `getEntityOptions`, and their corresponding setters. I18n methods include `getText`, `getTexts`, and `setText`. Auth helpers cover registration, login, token refresh, logout, password reset, current permissions and preferences, and user and role lists.

Methods take and return Jackson `JsonNode` values. `ChillSharpClientException` exposes `getStatusCode()` and `getResponseText()` for HTTP failures. Set a token at any time with `setAccessToken`; HTTP requests and SignalR connections use it as a bearer token.

## Entity change notifications

```java
try (ChillSharpClient.EntityChangeSubscription subscription = client.subscribeToEntityChanges(
        "Model.Post",
        changes -> changes.forEach(change -> System.out.println(change.getAction() + ": " + change.getGuid())))) {
    // Keep the subscription open while the application needs updates.
}

client.disconnectEntityChanges();
```

Omit the third `UUID` argument to `subscribeToEntityChanges` to subscribe to every change for the type. Pass an entity GUID to subscribe to one entity. The client shares its connection across subscriptions and restores registered groups after reconnecting.

## Limitations

The Java client currently provides explicit bearer-token handling. It does not yet manage username/password login or refresh tokens automatically. Attachment upload and download helpers are not included yet.
