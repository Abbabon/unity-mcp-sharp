using System.Collections.Generic;
using System;

using UnityEngine;
using UnityMCPSharp;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to find a GameObject by name, tag, or path.
    /// </summary>
    public static class FindGameObjectHandler
    {
        public static void Handle(string requestId, object parameters, MCPClient client)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<FindGameObjectData>(json);

                GameObject go = null;

                switch (data.searchBy.ToLower())
                {
                    case "tag":
                        go = GameObject.FindWithTag(data.name);
                        break;
                    case "path":
                        go = GameObject.Find(data.name);
                        break;
                    case "name":
                    default:
                        go = GameObject.Find(data.name);
                        break;
                }

                if (go != null)
                {
                    var components = new List<string>();
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        components.Add(comp.GetType().Name);
                    }

                    // Get full path in hierarchy
                    var path = go.name;
                    var parent = go.transform.parent;
                    while (parent != null)
                    {
                        path = parent.name + "/" + path;
                        parent = parent.parent;
                    }

                    var response = new
                    {
                        name = go.name,
                        path = path,
                        isActive = go.activeInHierarchy,
                        position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                        rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                        scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z },
                        components = components
                    };

                    _ = client.SendResponseAsync(requestId, response);
                }
                else
                {
                    Debug.LogWarning($"[FindGameObjectHandler] GameObject not found: {data.name}");
                    _ = client.SendResponseAsync(requestId, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FindGameObjectHandler] Error finding GameObject: {ex.Message}");
            }
        }
    }
}
