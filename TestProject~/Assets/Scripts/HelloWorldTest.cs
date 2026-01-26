using UnityEngine;

public class HelloWorldTest : MonoBehaviour
{
    private int logCount = 0;
    
    void Update()
    {
        if (logCount < 3)
        {
            Debug.Log("Hello World!");
            logCount++;
        }
    }
}