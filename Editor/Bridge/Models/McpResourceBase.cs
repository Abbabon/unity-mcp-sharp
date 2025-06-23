using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Editor.Bridge.Models
{
    /// <summary>
    /// Base class for MCP Unity resources that provide data from the Unity Editor
    /// </summary>
    public abstract class McpResourceBase
    {
        /// <summary>
        /// The name of the resource as used in API calls
        /// </summary>
        public string Name { get; protected set; }
        
        /// <summary>
        /// Description of the resource's functionality
        /// </summary>
        public string Description { get; protected set; }
        
        /// <summary>
        /// The URI pattern of the resource
        /// </summary>
        public string Uri { get; protected set; }
        
        /// <summary>
        /// Whether this resource is enabled and available for use
        /// </summary>
        public bool IsEnabled { get; protected set; } = true;
        
        /// <summary>
        /// Indicates if the fetch operation is asynchronous.
        /// </summary>
        public bool IsAsync { get; protected set; } = false;

        /// <summary>
        /// Synchronously fetch the resource data.
        /// Implement this for synchronous resources (IsAsync = false).
        /// </summary>
        /// <param name="parameters">Parameters extracted from the URI or query.</param>
        /// <returns>Result as JObject.</returns>
        public virtual JObject Fetch(JObject parameters)
        {
            // Default implementation throws, forcing sync resources to override.
            throw new NotImplementedException($"Synchronous Fetch not implemented for resource '{Name}'. Mark IsAsync=true and implement FetchAsync, or override Fetch.");
        }

        /// <summary>
        /// Asynchronously fetch the resource data.
        /// Implement this for asynchronous resources (IsAsync = true).
        /// The implementation MUST eventually call tcs.SetResult() or tcs.SetException().
        /// </summary>
        /// <param name="parameters">Parameters extracted from the URI or query.</param>
        /// <param name="tcs">TaskCompletionSource to set the result on.</param>
        public virtual void FetchAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            // Default implementation throws, forcing async resources to override.
            tcs.SetException(new NotImplementedException($"Asynchronous FetchAsync not implemented for resource '{Name}'. Mark IsAsync=false and implement Fetch, or override FetchAsync."));
        }
    }
}
