using UnityEngine;
namespace Mehmet{

public class SimpleMoveTest : MonoBehaviour
{
    public float speed = 3f;
    public Vector2 targetPosition = new Vector2(50, 50);
    
    void Update()
    {
        Vector2 currentPos = transform.position;
        Vector2 direction = (targetPosition - currentPos).normalized;
        
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        
        
        float distance = Vector2.Distance(currentPos, targetPosition);
        
        if (distance < 0.5f)
        {
            Debug.Log("Target reached!");
            enabled = false;
        }
        
        if (Time.frameCount % 60 == 0) 
        {
            Debug.Log($"Position: {currentPos}, Distance to target: {distance:F2}");
        }
    }
}
}