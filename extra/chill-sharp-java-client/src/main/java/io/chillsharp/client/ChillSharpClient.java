/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

package io.chillsharp.client;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.NullNode;

import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Objects;

/**
 * Lightweight client for the generic ChillSharp HTTP API. Payloads and responses use Jackson
 * {@link JsonNode} values, so applications do not need generated entity classes.
 */
public final class ChillSharpClient {
    public static final String VERSION = "0.1.0";
    public static final String API_BASE_PATH = "api";

    private final ObjectMapper mapper;
    private final HttpClient httpClient;
    private final String chillUrl;
    private final String apiUrl;
    private final String cultureName;
    private volatile String accessToken;

    public ChillSharpClient(String baseUrl) {
        this(baseUrl, null, null, null, HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(30)).build(), new ObjectMapper());
    }

    public ChillSharpClient(String baseUrl, String accessToken, String cultureName) {
        this(baseUrl, "api", accessToken, cultureName, HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(30)).build(), new ObjectMapper());
    }

    public ChillSharpClient(String baseUrl, String apiBasePath, String accessToken, String cultureName,
                            HttpClient httpClient, ObjectMapper mapper) {
        Objects.requireNonNull(baseUrl, "baseUrl");
        this.mapper = Objects.requireNonNull(mapper, "mapper");
        this.httpClient = Objects.requireNonNull(httpClient, "httpClient");
        this.cultureName = normalizeOptional(cultureName);
        this.accessToken = normalizeOptional(accessToken);
        String normalized = baseUrl.trim().replaceAll("/+$", "");
        if (normalized.isEmpty()) throw new IllegalArgumentException("baseUrl is required.");
        if (isEndpoint(normalized)) {
            this.chillUrl = normalized;
        } else {
            String path = apiBasePath == null ? API_BASE_PATH : apiBasePath.trim().replaceAll("^/+|/+$", "");
            if (path.isEmpty()) this.chillUrl = normalized + "/chill";
            else if (normalized.toLowerCase().endsWith("/" + path.toLowerCase())) this.chillUrl = normalized + "/chill";
            else this.chillUrl = normalized + "/" + path + "/chill";
        }
        this.apiUrl = this.chillUrl.toLowerCase().endsWith("/chill")
                ? this.chillUrl.substring(0, this.chillUrl.length() - 6)
                : this.chillUrl;
    }

    public String getVersion() { return VERSION; }
    public String getAccessToken() { return accessToken; }
    public void setAccessToken(String accessToken) { this.accessToken = normalizeOptional(accessToken); }

    public JsonNode query(JsonNode payload) { return post(chillUrl + "/query", payload); }
    public JsonNode lookup(JsonNode payload) { return post(chillUrl + "/lookup", payload); }
    public JsonNode find(JsonNode payload) { return post(chillUrl + "/find", payload); }
    public JsonNode create(JsonNode payload) { return post(chillUrl + "/create", payload); }
    public JsonNode update(JsonNode payload) { return post(chillUrl + "/update", payload); }
    public void delete(JsonNode payload) { send("POST", chillUrl + "/delete", payload, false); }
    public JsonNode autocomplete(JsonNode payload) { return post(chillUrl + "/autocomplete", payload); }
    public ArrayNode validate(JsonNode payload) {
        JsonNode result = post(chillUrl + "/validate", payload);
        return result instanceof ArrayNode array ? array : mapper.createArrayNode();
    }
    public ArrayNode chunk(JsonNode operations) {
        JsonNode result = post(chillUrl + "/chunk", operations);
        return result instanceof ArrayNode array ? array : mapper.createArrayNode();
    }

    public String test() { return send("GET", apiUrl + "/test", null, true).asText(); }

    public JsonNode getSchema(String chillType, String chillViewCode) { return getSchema(chillType, chillViewCode, null, false); }
    public JsonNode getSchema(String chillType, String chillViewCode, String culture, boolean update) {
        String url = schemaUrl() + "/get-schema?chillType=" + encode(required(chillType, "chillType"))
                + "&chillViewCode=" + encode(required(chillViewCode, "chillViewCode"));
        String effectiveCulture = normalizeOptional(culture) == null ? cultureName : normalizeOptional(culture);
        if (effectiveCulture != null) url += "&cultureName=" + encode(effectiveCulture);
        if (update) url += "&update=true";
        return send("GET", url, null, true);
    }
    public JsonNode setSchema(JsonNode schema) { return post(schemaUrl() + "/set-schema", schema); }
    public JsonNode getSchemaList() { return getSchemaList(null); }
    public JsonNode getSchemaList(String culture) {
        String effectiveCulture = normalizeOptional(culture) == null ? cultureName : normalizeOptional(culture);
        String url = schemaUrl() + "/get-schema-list" + (effectiveCulture == null ? "" : "?cultureName=" + encode(effectiveCulture));
        return send("GET", url, null, true);
    }
    public JsonNode getEntityOptions(String chillType) {
        return send("GET", schemaUrl() + "/get-entity-options?chillType=" + encode(required(chillType, "chillType")), null, false);
    }
    public JsonNode setEntityOptions(JsonNode options) { return post(schemaUrl() + "/set-entity-options", options); }

    public JsonNode getText(JsonNode request) { return post(i18nUrl() + "/get-text", request, true); }
    public JsonNode getTexts(ArrayNode requests) { return post(i18nUrl() + "/get-multiple-text", requests, true); }
    public JsonNode setText(JsonNode payload) { return send("PUT", i18nUrl() + "/set-text", payload, true); }

    public JsonNode registerAuthAccount(JsonNode payload) { return auth("POST", "register", payload, true); }
    public JsonNode loginAuthAccount(JsonNode payload) { return auth("POST", "login", payload, true); }
    public JsonNode logoutAuthAccount() {
        send("POST", authUrl() + "/logout", null, false);
        setAccessToken(null);
        return NullNode.getInstance();
    }
    public JsonNode refreshAuthAccount(JsonNode payload) { return auth("POST", "refresh", payload, true); }
    public JsonNode changeAuthPassword(JsonNode payload) { return auth("POST", "change-password", payload, false); }
    public JsonNode requestAuthPasswordReset(JsonNode payload) { return auth("POST", "request-password-reset", payload, true); }
    public JsonNode resetAuthPassword(JsonNode payload) { return auth("POST", "reset-password", payload, true); }
    public JsonNode getAuthPermissions() { return auth("GET", "get-permissions", null, false); }
    public JsonNode getCurrentUserPreferences() { return auth("GET", "current-user-preferences", null, false); }
    public JsonNode getAuthUserList() { return auth("GET", "get-user-list", null, false); }
    public JsonNode getAuthRoleList() { return auth("GET", "get-role-list", null, false); }

    private JsonNode auth(String method, String path, JsonNode payload, boolean anonymous) {
        JsonNode result = send(method, authUrl() + "/" + path, payload, anonymous);
        if (path.equals("login") || path.equals("register") || path.equals("refresh")) {
            JsonNode token = result == null ? null : result.hasNonNull("accessToken") ? result.get("accessToken") : result.get("AccessToken");
            if (token != null && !token.isNull()) setAccessToken(token.asText());
        }
        return result;
    }
    private JsonNode post(String url, JsonNode payload) { return post(url, payload, false); }
    private JsonNode post(String url, JsonNode payload, boolean anonymous) { return send("POST", url, payload, anonymous); }

    private JsonNode send(String method, String url, JsonNode payload, boolean anonymous) {
        try {
            HttpRequest.Builder builder = HttpRequest.newBuilder(URI.create(url)).timeout(Duration.ofSeconds(30))
                    .header("Accept", "application/json");
            if (!anonymous && accessToken != null) builder.header("Authorization", "Bearer " + accessToken);
            if (payload == null) builder.method(method, HttpRequest.BodyPublishers.noBody());
            else builder.header("Content-Type", "application/json; charset=utf-8")
                    .method(method, HttpRequest.BodyPublishers.ofString(mapper.writeValueAsString(payload)));
            HttpResponse<String> response = httpClient.send(builder.build(), HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
            if (response.statusCode() < 200 || response.statusCode() >= 300) {
                throw new ChillSharpClientException("HTTP " + response.statusCode() + " calling " + method + " " + url,
                        response.statusCode(), response.body());
            }
            String body = response.body();
            if (body == null || body.isBlank()) return NullNode.getInstance();
            try { return mapper.readTree(body); }
            catch (IOException ignored) { return mapper.getNodeFactory().textNode(body); }
        } catch (InterruptedException exception) {
            Thread.currentThread().interrupt();
            throw new ChillSharpClientException("Request interrupted: " + method + " " + url, exception);
        } catch (IOException | IllegalArgumentException exception) {
            throw new ChillSharpClientException("Request failed: " + method + " " + url, exception);
        }
    }

    private String authUrl() { return siblingUrl("chill-auth"); }
    private String schemaUrl() { return siblingUrl("chill-schema"); }
    private String i18nUrl() { return siblingUrl("chill-i18n"); }
    private String siblingUrl(String endpoint) {
        if (chillUrl.toLowerCase().endsWith("/chill")) return chillUrl.substring(0, chillUrl.length() - 6) + "/" + endpoint;
        return chillUrl + "-" + endpoint.substring("chill-".length());
    }
    private static boolean isEndpoint(String url) {
        String value = url.toLowerCase();
        return value.endsWith("/chill") || value.endsWith("/chill-auth") || value.endsWith("/chill-schema")
                || value.endsWith("/chill-i18n") || value.endsWith("/chill-attachment");
    }
    private static String encode(String value) { return URLEncoder.encode(value, StandardCharsets.UTF_8).replace("+", "%20"); }
    private static String required(String value, String name) {
        String normalized = normalizeOptional(value);
        if (normalized == null) throw new IllegalArgumentException(name + " is required.");
        return normalized;
    }
    private static String normalizeOptional(String value) { return value == null || value.isBlank() ? null : value.trim(); }
}
