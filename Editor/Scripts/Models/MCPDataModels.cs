using System;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Models
{
    /// <summary>
    /// Data models for MCP requests and responses.
    /// All models use Newtonsoft.Json for serialization to ensure compatibility with WebSocket messages.
    /// </summary>

    [Serializable]
    public class LogEntry
    {
        [JsonProperty("type")]
        public string type;
        [JsonProperty("message")]
        public string message;
        [JsonProperty("stackTrace")]
        public string stackTrace;
    }

    [Serializable]
    public class SceneObject
    {
        [JsonProperty("name")]
        public string name;
        [JsonProperty("isActive")]
        public bool isActive;
        [JsonProperty("depth")]
        public int depth;
    }

    [Serializable]
    public class CreateGameObjectData
    {
        [JsonProperty("name")]
        public string name;
        [JsonProperty("position")]
        public PositionData position;
        [JsonProperty("components")]
        public string[] components;
        [JsonProperty("parent")]
        public string parent;
    }

    [Serializable]
    public class CreateScriptData
    {
        [JsonProperty("scriptName")]
        public string scriptName;
        [JsonProperty("folderPath")]
        public string folderPath;
        [JsonProperty("scriptContent")]
        public string scriptContent;
    }

    [Serializable]
    public class AddComponentData
    {
        [JsonProperty("gameObjectName")]
        public string gameObjectName;
        [JsonProperty("componentType")]
        public string componentType;
    }

    [Serializable]
    public class SetComponentFieldData
    {
        [JsonProperty("gameObjectName")]
        public string gameObjectName;
        [JsonProperty("componentType")]
        public string componentType;
        [JsonProperty("fieldName")]
        public string fieldName;
        [JsonProperty("value")]
        public string value;
        [JsonProperty("valueType")]
        public string valueType;
    }

    [Serializable]
    public class CreateAssetData
    {
        [JsonProperty("assetName")]
        public string assetName;
        [JsonProperty("folderPath")]
        public string folderPath;
        [JsonProperty("assetTypeName")]
        public string assetTypeName;
        [JsonProperty("propertiesJson")]
        public string propertiesJson;
    }

    [Serializable]
    public class PositionData
    {
        [JsonProperty("x")]
        public float x;
        [JsonProperty("y")]
        public float y;
        [JsonProperty("z")]
        public float z;
    }

    [Serializable]
    public class BatchCreateData
    {
        [JsonProperty("gameObjectsJson")]
        public string gameObjectsJson;
    }

    [Serializable]
    public class FindGameObjectData
    {
        [JsonProperty("name")]
        public string name;
        [JsonProperty("searchBy")]
        public string searchBy;
    }

    [Serializable]
    public class OpenSceneData
    {
        [JsonProperty("scenePath")]
        public string scenePath;
        [JsonProperty("additive")]
        public bool additive;
    }

    [Serializable]
    public class CloseSceneData
    {
        [JsonProperty("sceneIdentifier")]
        public string sceneIdentifier;
    }

    [Serializable]
    public class SetActiveSceneData
    {
        [JsonProperty("sceneIdentifier")]
        public string sceneIdentifier;
    }

    [Serializable]
    public class SaveSceneData
    {
        [JsonProperty("scenePath")]
        public string scenePath;
        [JsonProperty("saveAll")]
        public bool saveAll;
    }

    [Serializable]
    public class CreateGameObjectInSceneData
    {
        [JsonProperty("scenePath")]
        public string scenePath;
        [JsonProperty("name")]
        public string name;
        [JsonProperty("position")]
        public PositionData position;
        [JsonProperty("components")]
        public string[] components;
        [JsonProperty("parent")]
        public string parent;
    }
}
