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

using Microsoft.AspNetCore.SignalR;

namespace ChillSharp.Api
{
    public interface IChillEntityChangeDispatcher
    {
        Task DispatchAsync(IReadOnlyCollection<ChillEntityChangeNotification> changes, CancellationToken cancellationToken = default);
    }

    internal sealed class ChillEntityChangeDispatcher : IChillEntityChangeDispatcher
    {
        private readonly IHubContext<ChillEntityChangeHub> _hubContext;

        public ChillEntityChangeDispatcher(IHubContext<ChillEntityChangeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DispatchAsync(IReadOnlyCollection<ChillEntityChangeNotification> changes, CancellationToken cancellationToken = default)
        {
            if (changes.Count == 0)
                return;

            var uniqueChanges = changes
                .Where(x => !string.IsNullOrWhiteSpace(x.ChillType) && x.Guid != Guid.Empty && !string.IsNullOrWhiteSpace(x.Action))
                .GroupBy(x => (x.ChillType, x.Guid, x.Action))
                .Select(group => group.First())
                .ToList();

            if (uniqueChanges.Count == 0)
                return;

            foreach (var typeGroup in uniqueChanges.GroupBy(x => x.ChillType, StringComparer.Ordinal))
            {
                var payload = typeGroup
                    .OrderBy(x => x.Guid)
                    .ToArray();

                await _hubContext.Clients
                    .Group(ChillEntityChangeHub.BuildGroupName(typeGroup.Key))
                    .SendAsync(ChillEntityChangeHub.NotificationMethodName, payload, cancellationToken);
            }

            foreach (var change in uniqueChanges)
            {
                await _hubContext.Clients
                    .Group(ChillEntityChangeHub.BuildGroupName(change.ChillType, change.Guid))
                    .SendAsync(
                        ChillEntityChangeHub.NotificationMethodName,
                        new[] { change },
                        cancellationToken);
            }
        }
    }
}
