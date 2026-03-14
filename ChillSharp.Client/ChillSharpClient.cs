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

using ChillSharp.Client.Dto;
using System.Text;
using System.Text.Json;

namespace ChillSharp.Client
{
    /// <summary>
    /// Lightweight client for interacting with the ChillSharp API.
    /// Provides methods for querying, CRUD operations, and batch actions.
    /// </summary>
    public partial class ChillSharpClient
    {
        private string _BaseUrl = string.Empty;

        /// <summary>
        /// Initializes the client with the base URL of the ChillSharp API.
        /// Removes trailing slashes for consistent request formatting.
        /// </summary>
        /// <param name="BaseUrl">Base endpoint of the ChillSharp server.</param>
        public ChillSharpClient(string BaseUrl)
        { 
            if (BaseUrl.EndsWith("/"))
                BaseUrl = BaseUrl.Substring(0, BaseUrl.Length - 1);
            _BaseUrl = BaseUrl;
        }

        /// <summary>
        /// Sends a query request to the ChillSharp API.
        /// </summary>
        /// <param name="Query">Query DTO defining filters and parameters.</param>
        /// <returns>The response mapped back into a ChillDtoQuery object.</returns>
        public ChillDtoQuery Query(ChillDtoQuery Query)
        {
            DateTime start = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{_BaseUrl}/query";
            using (HttpClient client = new HttpClient())
            {
                string jsonString = JsonSerializer.Serialize(Query);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                try
                {
                    // Send the POST request
                    var req = client.PostAsync(url, content);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    // Check the response
                    if (response.IsSuccessStatusCode)
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string responseBody = res.Result;
                        var ret = JsonSerializer.Deserialize<ChillDtoQuery>(responseBody, options);
                        Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                        if (ret == null) throw new ChillClientException("Unexpected null query result");
                        return ret;
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing {Action}, see inner exception for details", ex);
                }
            }
        }

        /// <summary>
        /// Executes a FIND operation on the given entity.
        /// </summary>
        public ChillDtoEntity? Find(ChillDtoEntity Entity)
        {
            return Action("FIND", Entity);
        }

        /// <summary>
        /// Executes a CREATE operation on the given entity.
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        /// </summary>
        public ChillDtoEntity Create(ChillDtoEntity Entity)
        {
            var res = Action("CREATE", Entity);
            if (res == null) throw new ChillClientException("Unexpected null entity result");
            return res;
        }

        /// <summary>
        /// Executes an UPDATE operation on the given entity.
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        /// </summary>
        public ChillDtoEntity Update(ChillDtoEntity Entity)
        {
            var res = Action("UPDATE", Entity);
            if (res == null) throw new ChillClientException("Unexpected null entity result");
            return res;
        }

        /// <summary>
        /// Executes a DELETE operation on the given entity.
        /// <param name="Entity">Entity payload for the action.</param>
        /// </summary>
        public void Delete(ChillDtoEntity Entity)
        {
            Action("DELETE", Entity);
        }

        /// <summary>
        /// Internal method used by the CRUD helpers to send
        /// an action-based request to the API.
        /// </summary>
        /// <param name="Action">Action verb (FIND, CREATE, UPDATE, DELETE).</param>
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        protected ChillDtoEntity? Action(string Action, ChillDtoEntity Entity)
        {
            DateTime start = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{_BaseUrl}/{Action.ToLowerInvariant()}";
            using (HttpClient client = new HttpClient())
            {
                string jsonString = JsonSerializer.Serialize(Entity, options);

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                try
                {
                    // Send the POST request
                    var req = client.PostAsync(url, content);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    // Check the response
                    if (response.IsSuccessStatusCode)
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string responseBody = res.Result;
                        if (Action.ToLowerInvariant() != "delete")
                        {
                            var ret = JsonSerializer.Deserialize<ChillDtoEntity>(responseBody, options);
                            Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                            return ret;
                        }
                        else
                        {
                            Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                            return null;
                        }
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing {Action}, see inner exception for details", ex);
                }
            }
        }

        /// <summary>
        /// Sends a batch (chunk) of ChillOperation objects to the API.
        /// </summary>
        /// <param name="Chunk">List of operations to process.</param>
        /// <returns>The processed operations returned by the server.</returns>
        public List<ChillOperation> Chunk(List<ChillOperation> Chunk)
        {
            DateTime start = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{_BaseUrl}/chunk";
            using (HttpClient client = new HttpClient())
            {
                string jsonString = JsonSerializer.Serialize(Chunk, options);

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                try
                {
                    // Send the POST request
                    var req = client.PostAsync(url, content);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    // Check the response
                    if (response.IsSuccessStatusCode)
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string responseBody = res.Result;
                        var ret = JsonSerializer.Deserialize<List<ChillOperation>>(responseBody, options);
                            Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                            return ret;
                        }
                        else
                        {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing {Action}, see inner exception for details", ex);
                }
            }
        }

        /// <summary>
        /// Retrieves the schema definition for a specified chill type and view code from the remote service.
        /// </summary>
        /// <param name="chillType">The identifier of the chill type for which to retrieve the schema. Cannot be null or empty.</param>
        /// <param name="chillViewCode">The code representing the specific view of the chill type. Cannot be null or empty.</param>
        /// <returns>A <see cref="ChillDtoSchema"/> object containing the schema definition if found; otherwise, <see
        /// langword="null"/>.</returns>
        /// <exception cref="ChillClientException">Thrown if the remote service returns an error response or if an unexpected error occurs during the request.</exception>
        public ChillDtoSchema? GetSchema(string chillType, string chillViewCode)
        {
            DateTime start = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{_BaseUrl}/get-schema?chillType={chillType}&chillViewCode={chillViewCode}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send the GET request
                    var req = client.GetAsync(url);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    // Check the response
                    if (response.IsSuccessStatusCode)
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string responseBody = res.Result;
                        var ret = JsonSerializer.Deserialize<ChillDtoSchema>(responseBody, options);
                        Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                        return ret;
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing {Action}, see inner exception for details", ex);
                }
            }
        }

        /// <summary>
        /// Sends a schema definition to the remote service.
        /// </summary>
        /// <param name="schema">
        /// The <see cref="ChillDtoSchema"/> object containing the schema definition,
        /// including chillType and chillViewCode. Cannot be null.
        /// </param>
        /// <exception cref="ChillClientException">
        /// Thrown if the remote service returns an error response or if an unexpected error occurs during the request.
        /// </exception>
        public void SetSchema(ChillDtoSchema schema)
        {
            DateTime start = DateTime.Now;

            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{_BaseUrl}/set-schema";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string json = JsonSerializer.Serialize(schema, options);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var req = client.PostAsync(url, content);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing {Action}, see inner exception for details", ex);
                }
            }
        }
    }
}
