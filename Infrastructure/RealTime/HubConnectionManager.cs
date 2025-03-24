using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MonkMode.Infrastructure.RealTime
{
    public class HubConnectionManager
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        public void AddConnection(string userId, string connectionId)
        {
            _userConnections.AddOrUpdate(
                userId,
                new HashSet<string> { connectionId },
                (_, connections) =>
                {
                    connections.Add(connectionId);
                    return connections;
                });
        }

        public void RemoveConnection(string userId, string connectionId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }

        public IEnumerable<string> GetConnectionsForUser(string userId)
        {
            return _userConnections.TryGetValue(userId, out var connections)
                ? connections
                : new HashSet<string>();
        }

        public IEnumerable<string> GetConnectionsForUsers(IEnumerable<string> userIds)
        {
            var connections = new HashSet<string>();

            foreach (var userId in userIds)
            {
                if (_userConnections.TryGetValue(userId, out var userConnections))
                {
                    foreach (var connection in userConnections)
                    {
                        connections.Add(connection);
                    }
                }
            }

            return connections;
        }
    }
}