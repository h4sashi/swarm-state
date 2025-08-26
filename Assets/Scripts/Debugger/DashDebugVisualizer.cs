using UnityEngine;

public class DashDebugVisualizer : MonoBehaviour
{
    [SerializeField] private PlayerDash dash;
    [SerializeField] private LineRenderer lineRenderer;
    
    void Start()
    {
        if (!lineRenderer)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.red;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 2;
        }
        
        GameEvents.OnPlayerDash += ShowDashLine;
    }
    
    private void ShowDashLine(Vector3 startPos)
    {
        if (!dash.IsDashing) return;
        
        Vector3 dashDirection = GetComponent<IMovementController>().GetMovementDirection();
        if (dashDirection.magnitude < 0.1f)
            dashDirection = transform.forward;
            
        Vector3 endPos = startPos + dashDirection * 3f; // Visualize 3 units ahead
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        lineRenderer.enabled = true;
        
        Invoke(nameof(HideDashLine), 0.5f);
    }
    
    private void HideDashLine()
    {
        lineRenderer.enabled = false;
    }
}