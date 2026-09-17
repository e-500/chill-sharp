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

using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ChillSharp.Tests;

[TestClass]
public sealed class PerformanceApi
{
    private const int TotalBlogs = 10_000;
    private const int TitleLength = 128;
    private const int InitialChunkSize = 128;
    private const int MinChunkSize = 1;
    private const int MaxChunkSize = 8_192;
    private const int QueryPageSize = 2_048;
    private static readonly TimeSpan IncreaseThreshold = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DecreaseThreshold = TimeSpan.FromSeconds(5);
    private static readonly char[] TitleAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Performance")]
    [Timeout(60 * 60 * 1000)]
    public async Task Step001_Insert100kBlogsOverHttpsWithAdaptiveChunkSizing()
    {
        TestApiHost.EnsureHttpsStarted();

        var client = CreateHttpsChillClient();
        var measurements = new List<ChunkMeasurement>();
        var insertedBlogs = 0;
        var nextChunkSize = InitialChunkSize;
        var totalStopwatch = Stopwatch.StartNew();

        while (insertedBlogs < TotalBlogs)
        {
            var chunkSize = Math.Min(nextChunkSize, TotalBlogs - insertedBlogs);
            var chunk = BuildCreateChunk(insertedBlogs, chunkSize);

            var stopwatch = Stopwatch.StartNew();
            var response = client.Chunk(chunk);
            stopwatch.Stop();

            Assert.IsNotNull(response);
            Assert.AreEqual(chunk.Count, response.Count);

            measurements.Add(new ChunkMeasurement(chunkSize, stopwatch.Elapsed));
            insertedBlogs += chunkSize;
            nextChunkSize = AdjustChunkSize(nextChunkSize, stopwatch.Elapsed);

            TestContext.WriteLine(
                $"Inserted {insertedBlogs}/{TotalBlogs} blogs. " +
                $"ChunkSize={chunkSize}, DurationMs={stopwatch.Elapsed.TotalMilliseconds:F0}, NextChunkSize={nextChunkSize}");
        }

        totalStopwatch.Stop();

        await using var db = TestApiHost.CreateHttpsDbContext();
        var insertedCount = await db.Blog.CountAsync();
        Assert.AreEqual(TotalBlogs, insertedCount);

        PrintSummary(measurements, totalStopwatch.Elapsed);
    }

    [TestMethod]
    [TestCategory("Performance")]
    [Timeout(60 * 60 * 1000)]
    public async Task Step002_ReadAndUpdateBlogsOverHttpsWithPipelinedChunkPreparation()
    {
        TestApiHost.EnsureHttpsStarted();
        await EnsureBlogsExistAsync();

        var queryClient = CreateHttpsChillClient();
        var updateClient = CreateHttpsChillClient();
        var measurements = new List<UpdateChunkMeasurement>();
        var searchMeasurements = new List<QueryChunkMeasurement>();
        var updatedBlogs = 0;
        var nextChunkSize = InitialChunkSize;
        var totalStopwatch = Stopwatch.StartNew();
        var nextPage = 1;
        var hasMorePages = true;
        var initialQuery = QueryBlogChunk(queryClient, nextPage, QueryPageSize);
        searchMeasurements.Add(initialQuery);
        var pendingBlogs = new Queue<ChillDtoEntity>(initialQuery.Blogs);
        Assert.IsTrue(pendingBlogs.Count > 0, "Expected blogs to update.");
        nextPage++;
        if (pendingBlogs.Count < QueryPageSize)
        {
            hasMorePages = false;
        }

        Task<QueryChunkMeasurement>? prefetchedPageTask = null;

        while (pendingBlogs.Count > 0 || prefetchedPageTask != null || hasMorePages)
        {
            if (prefetchedPageTask == null && hasMorePages)
            {
                prefetchedPageTask = Task.Run(() => QueryBlogChunk(queryClient, nextPage, QueryPageSize));
                nextPage++;
            }

            if (pendingBlogs.Count == 0)
            {
                var fetchedPage = await ConsumePrefetchedPageAsync(prefetchedPageTask);
                prefetchedPageTask = null;
                searchMeasurements.Add(fetchedPage);
                EnqueueBlogs(pendingBlogs, fetchedPage.Blogs);
                if (fetchedPage.Blogs.Count < QueryPageSize)
                {
                    hasMorePages = false;
                }

                if (pendingBlogs.Count == 0)
                {
                    break;
                }
            }

            var currentChunk = DequeueBlogs(pendingBlogs, nextChunkSize);

            var updateStopwatch = Stopwatch.StartNew();
            var response = updateClient.Chunk(BuildUpdateChunk(currentChunk));
            updateStopwatch.Stop();

            Assert.IsNotNull(response);
            Assert.AreEqual(currentChunk.Count + 2, response.Count);

            if (prefetchedPageTask != null && prefetchedPageTask.IsCompleted)
            {
                var fetchedPage = await ConsumePrefetchedPageAsync(prefetchedPageTask);
                prefetchedPageTask = null;
                searchMeasurements.Add(fetchedPage);
                EnqueueBlogs(pendingBlogs, fetchedPage.Blogs);
                if (fetchedPage.Blogs.Count < QueryPageSize)
                {
                    hasMorePages = false;
                }
            }

            measurements.Add(new UpdateChunkMeasurement(currentChunk.Count, updateStopwatch.Elapsed));
            updatedBlogs += currentChunk.Count;
            nextChunkSize = AdjustChunkSize(nextChunkSize, updateStopwatch.Elapsed);

            TestContext.WriteLine(
                $"Updated {updatedBlogs}/{TotalBlogs} blogs. " +
                $"ChunkSize={currentChunk.Count}, UpdateMs={updateStopwatch.Elapsed.TotalMilliseconds:F0}, NextChunkSize={nextChunkSize}, Buffered={pendingBlogs.Count}");
        }

        totalStopwatch.Stop();

        await using var db = TestApiHost.CreateHttpsDbContext();
        var updatedCount = await db.Blog.CountAsync(x => x.Title != null && x.Title.Length == TitleLength);
        Assert.AreEqual(TotalBlogs, updatedCount);

        PrintUpdateSummary(measurements, searchMeasurements, totalStopwatch.Elapsed);
    }

    private static ChillSharpClient CreateHttpsChillClient()
    {
        return new ChillSharpClient(
            $"{TestApiHost.HttpsBaseUrl}api/chill",
            () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                return new HttpClient(handler);
            });
    }

    private async Task EnsureBlogsExistAsync()
    {
        await using var db = TestApiHost.CreateHttpsDbContext();
        var existingBlogs = await db.Blog.CountAsync();
        if (existingBlogs == TotalBlogs)
        {
            return;
        }

        await Step001_Insert100kBlogsOverHttpsWithAdaptiveChunkSizing();
    }

    private static List<ChillOperation> BuildCreateChunk(int offset, int chunkSize)
    {
        var chunk = new List<ChillOperation>(chunkSize + 2)
        {
            new()
            {
                Index = 0,
                Verb = ChillOperationVerb.TRANSACTION
            }
        };

        for (var i = 0; i < chunkSize; i++)
        {
            var blogNumber = offset + i + 1;
            chunk.Add(new ChillOperation
            {
                Index = i + 1,
                Verb = ChillOperationVerb.CREATE,
                Entity = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = CreateRandomTitle(),
                        ["Url"] = $"https://perf.example/blog/{blogNumber:D6}"
                    }
                }
            });
        }

        chunk.Add(new ChillOperation
        {
            Index = chunk.Count,
            Verb = ChillOperationVerb.COMMIT
        });

        return chunk;
    }

    private static QueryChunkMeasurement QueryBlogChunk(ChillSharpClient client, int page, int chunkSize)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = new ChillDtoQuery
        {
            ChillType = "Query.BlogQuery",
            Pagination = new ChillPagination
            {
                Page = page,
                PageResults = chunkSize
            },
            ResultProperties = ChillDtoProperty.Build(["Guid", "Title"])
        };

        var response = client.Query(query);
        stopwatch.Stop();
        return new QueryChunkMeasurement(response.Results ?? new List<ChillDtoEntity>(), stopwatch.Elapsed);
    }

    private static async Task<QueryChunkMeasurement> ConsumePrefetchedPageAsync(Task<QueryChunkMeasurement>? prefetchedPageTask)
    {
        if (prefetchedPageTask == null)
        {
            return new QueryChunkMeasurement(new List<ChillDtoEntity>(), TimeSpan.Zero);
        }

        return await prefetchedPageTask;
    }

    private static void EnqueueBlogs(Queue<ChillDtoEntity> queue, IEnumerable<ChillDtoEntity> blogs)
    {
        foreach (var blog in blogs)
        {
            queue.Enqueue(blog);
        }
    }

    private static List<ChillDtoEntity> DequeueBlogs(Queue<ChillDtoEntity> queue, int chunkSize)
    {
        var size = Math.Min(chunkSize, queue.Count);
        var blogs = new List<ChillDtoEntity>(size);
        for (var i = 0; i < size; i++)
        {
            blogs.Add(queue.Dequeue());
        }

        return blogs;
    }

    private static List<ChillOperation> BuildUpdateChunk(List<ChillDtoEntity> blogs)
    {
        var chunk = new List<ChillOperation>(blogs.Count + 2)
        {
            new()
            {
                Index = 0,
                Verb = ChillOperationVerb.TRANSACTION
            }
        };

        for (var i = 0; i < blogs.Count; i++)
        {
            var blog = blogs[i];
            chunk.Add(new ChillOperation
            {
                Index = i + 1,
                Verb = ChillOperationVerb.UPDATE,
                Entity = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = blog.Guid,
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = CreateRandomTitle()
                    }
                }
            });
        }

        chunk.Add(new ChillOperation
        {
            Index = chunk.Count,
            Verb = ChillOperationVerb.COMMIT
        });

        return chunk;
    }

    private static int AdjustChunkSize(int currentChunkSize, TimeSpan elapsed)
    {
        if (elapsed < IncreaseThreshold)
        {
            return Math.Min(MaxChunkSize, currentChunkSize * 2);
        }

        if (elapsed > DecreaseThreshold)
        {
            return Math.Max(MinChunkSize, currentChunkSize / 2);
        }

        return currentChunkSize;
    }

    private static string CreateRandomTitle()
    {
        Span<char> buffer = stackalloc char[TitleLength];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = TitleAlphabet[RandomNumberGenerator.GetInt32(TitleAlphabet.Length)];
        }

        return new string(buffer);
    }

    private void PrintSummary(List<ChunkMeasurement> measurements, TimeSpan totalElapsed)
    {
        Assert.IsTrue(measurements.Count > 0);

        var minDurationMs = measurements.Min(x => x.Duration.TotalMilliseconds);
        var maxDurationMs = measurements.Max(x => x.Duration.TotalMilliseconds);
        var avgDurationMs = measurements.Average(x => x.Duration.TotalMilliseconds);

        var minChunkSize = measurements.Min(x => x.ChunkSize);
        var maxChunkSize = measurements.Max(x => x.ChunkSize);
        var avgChunkSize = measurements.Average(x => x.ChunkSize);

        var throughput = TotalBlogs / totalElapsed.TotalSeconds;

        TestContext.WriteLine("Performance summary");
        TestContext.WriteLine($"Chunks={measurements.Count}");
        TestContext.WriteLine($"DurationMs min={minDurationMs:F0} max={maxDurationMs:F0} avg={avgDurationMs:F0}");
        TestContext.WriteLine($"ChunkSize min={minChunkSize} max={maxChunkSize} avg={avgChunkSize:F1}");
        TestContext.WriteLine($"TotalSeconds={totalElapsed.TotalSeconds:F2}");
        TestContext.WriteLine($"BlogsPerSecond={throughput:F2}");
    }

    private void PrintUpdateSummary(List<UpdateChunkMeasurement> measurements, List<QueryChunkMeasurement> searchMeasurements, TimeSpan totalElapsed)
    {
        Assert.IsTrue(measurements.Count > 0);
        Assert.IsTrue(searchMeasurements.Count > 0);

        var minUpdateMs = measurements.Min(x => x.UpdateDuration.TotalMilliseconds);
        var maxUpdateMs = measurements.Max(x => x.UpdateDuration.TotalMilliseconds);
        var avgUpdateMs = measurements.Average(x => x.UpdateDuration.TotalMilliseconds);

        var minSearchMs = searchMeasurements.Min(x => x.Duration.TotalMilliseconds);
        var maxSearchMs = searchMeasurements.Max(x => x.Duration.TotalMilliseconds);
        var avgSearchMs = searchMeasurements.Average(x => x.Duration.TotalMilliseconds);

        var minChunkSize = measurements.Min(x => x.ChunkSize);
        var maxChunkSize = measurements.Max(x => x.ChunkSize);
        var avgChunkSize = measurements.Average(x => x.ChunkSize);

        var throughput = TotalBlogs / totalElapsed.TotalSeconds;

        TestContext.WriteLine("Update performance summary");
        TestContext.WriteLine($"Chunks={measurements.Count}");
        TestContext.WriteLine($"UpdateMs min={minUpdateMs:F0} max={maxUpdateMs:F0} avg={avgUpdateMs:F0}");
        TestContext.WriteLine($"SearchMs min={minSearchMs:F0} max={maxSearchMs:F0} avg={avgSearchMs:F0}");
        TestContext.WriteLine($"ChunkSize min={minChunkSize} max={maxChunkSize} avg={avgChunkSize:F1}");
        TestContext.WriteLine($"TotalSeconds={totalElapsed.TotalSeconds:F2}");
        TestContext.WriteLine($"BlogsPerSecond={throughput:F2}");
    }

    private sealed record ChunkMeasurement(int ChunkSize, TimeSpan Duration);

    private sealed record UpdateChunkMeasurement(int ChunkSize, TimeSpan UpdateDuration);

    private sealed record QueryChunkMeasurement(List<ChillDtoEntity> Blogs, TimeSpan Duration);
}
