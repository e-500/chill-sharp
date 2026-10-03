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

/** Raised when a ChillSharp HTTP request fails. */
public class ChillSharpClientException extends RuntimeException {
    private final int statusCode;
    private final String responseText;

    public ChillSharpClientException(String message, int statusCode, String responseText) {
        super(message);
        this.statusCode = statusCode;
        this.responseText = responseText;
    }

    public ChillSharpClientException(String message, Throwable cause) {
        super(message, cause);
        this.statusCode = 0;
        this.responseText = "";
    }

    public int getStatusCode() { return statusCode; }
    public String getResponseText() { return responseText; }
}
