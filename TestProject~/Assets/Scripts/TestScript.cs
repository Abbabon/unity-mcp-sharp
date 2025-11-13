using UnityEngine;

/// <summary>
/// Test script created to verify MCP server resilience during recompilation
/// </summary>
public class TestScript : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 45f;

    private void Start()
    {
        Debug.Log($"[TestScript] Initialized on {gameObject.name}");
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
