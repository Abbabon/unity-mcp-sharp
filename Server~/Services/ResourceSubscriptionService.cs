using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace UnityMcpServer.Services;

/// <summary>
/// Manages resource subscriptions and sends update notifications to MCP clients
/// when Unity state changes.
/// </summary>
public class ResourceSubscriptionService
{
private readonly ILogger<ResourceSubscriptionService> _logger;
private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new();

public ResourceSubscriptionService(ILogger<ResourceSubscriptionService> logger)
{
_logger = logger;
}

/// <summary>
/// Subscribe to a resource URI. Returns true if subscription was added.
/// </summary>
public bool Subscribe(string resourceUri, string clientId = "default")
{
var subscribers = _subscriptions.GetOrAdd(resourceUri, _ => new HashSet<string>());
lock (subscribers)
{
var added = subscribers.Add(clientId);
if (added)
{
_logger.LogInformation("Client '{ClientId}' subscribed to resource '{ResourceUri}'", clientId, resourceUri);
}
return added;
}
}

/// <summary>
/// Unsubscribe from a resource URI. Returns true if subscription was removed.
/// </summary>
public bool Unsubscribe(string resourceUri, string clientId = "default")
{
if (_subscriptions.TryGetValue(resourceUri, out var subscribers))
{
lock (subscribers)
{
var removed = subscribers.Remove(clientId);
if (removed)
{
_logger.LogInformation("Client '{ClientId}' unsubscribed from resource '{ResourceUri}'", clientId, resourceUri);
}

// Remove empty subscription set
if (subscribers.Count == 0)
{
_subscriptions.TryRemove(resourceUri, out _);
}

return removed;
}
}
return false;
}

/// <summary>
/// Get all client IDs subscribed to a resource URI.
/// </summary>
public IReadOnlyCollection<string> GetSubscribers(string resourceUri)
{
if (_subscriptions.TryGetValue(resourceUri, out var subscribers))
{
lock (subscribers)
{
return subscribers.ToList();
}
}
return Array.Empty<string>();
}

/// <summary>
/// Check if any clients are subscribed to a resource URI.
/// </summary>
public bool HasSubscribers(string resourceUri)
{
return _subscriptions.TryGetValue(resourceUri, out var subscribers) && subscribers.Count > 0;
}

/// <summary>
/// Get all resource URIs that have active subscriptions.
/// </summary>
public IReadOnlyCollection<string> GetSubscribedResources()
{
return _subscriptions.Keys.ToList();
}

/// <summary>
/// Clear all subscriptions for a client.
/// </summary>
public void ClearClientSubscriptions(string clientId)
{
var toRemove = new List<string>();

foreach (var (resourceUri, subscribers) in _subscriptions)
{
lock (subscribers)
{
subscribers.Remove(clientId);
if (subscribers.Count == 0)
{
toRemove.Add(resourceUri);
}
}
}

foreach (var uri in toRemove)
{
_subscriptions.TryRemove(uri, out _);
}

_logger.LogInformation("Cleared all subscriptions for client '{ClientId}'", clientId);
}

/// <summary>
/// Get count of active subscriptions.
/// </summary>
public int GetSubscriptionCount()
{
return _subscriptions.Sum(kvp => kvp.Value.Count);
}
}
