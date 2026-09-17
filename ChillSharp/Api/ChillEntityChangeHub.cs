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

using ChillSharp.Dto;
using Microsoft.AspNetCore.SignalR;

namespace ChillSharp.Api
{
    /// <summary>
    /// SignalR hub used by clients to subscribe to chill entity change notifications.
    /// </summary>
    public sealed class ChillEntityChangeHub : Hub
    {
        public const string NotificationMethodName = "EntitiesChanged";
        public const string HubRouteSuffix = "notify";

        private readonly IChillContext _context;

        public ChillEntityChangeHub(IChillContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Subscribes the current connection to all changes for the specified chill type or to a single entity.
        /// </summary>
        public Task Register(string chillType, Guid? guid = null)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(NormalizeChillType(chillType), guid));
        }

        /// <summary>
        /// Removes the current connection from a previous subscription.
        /// </summary>
        public Task Unregister(string chillType, Guid? guid = null)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroupName(NormalizeChillType(chillType), guid));
        }

        internal static string BuildGroupName(string chillType, Guid? guid = null)
        {
            return guid.HasValue
                ? $"entity:{chillType}:{guid.Value:N}"
                : $"type:{chillType}";
        }

        private string NormalizeChillType(string chillType)
        {
            if (string.IsNullOrWhiteSpace(chillType))
                throw new HubException("ChillType is required.");

            var resolvedType = ChillTypeResolver.ResolveType(_context.GetType().Assembly, chillType, _context.GetChillTypePrefix());
            return ChillTypeResolver.NormalizeChillType(resolvedType, _context.GetChillTypePrefix());
        }
    }
}
