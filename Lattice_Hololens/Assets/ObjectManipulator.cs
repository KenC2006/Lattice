using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    [Header("Manipulation Settings")]
    public GameObject ManipulatedObject;
    public float ManipulationSensitivity = 0.25f;
    public float RotationSensitivity = 10.0f;
    public float ScaleSensitivity = 0.01f;

    private bool isFlipped = false;

    void Start()
    {
        SubscribeToEvents();
    }

    void Update()
    {
        ExecuteManipulations();
    }

    private void SubscribeToEvents()
    {
        var actionManager = ActionManager.Instance;
        actionManager.ResetEvent += ResetObjectTransform;
        actionManager.FlipEvent += FlipObjectVertically;
    }

    private void ExecuteManipulations()
    {
        ProcessMovement();
        ProcessRotation();
        ProcessScaling();
    }

    private void ProcessMovement()
    {
        if (ManipulatedObject == null || !GestureManager.Instance.IsManipulating) return;
        
        Vector3 movement = ManipulationSensitivity * GestureManager.Instance.ManipulationPosition;
        ManipulatedObject.transform.position += movement;
    }

    private void ProcessRotation()
    {
        if (ManipulatedObject == null || !ShouldProcessRotation()) return;
        
        float rotationAmount = RotationSensitivity * GestureManager.Instance.NavigationPosition.x;
        ManipulatedObject.transform.Rotate(0, rotationAmount, 0);
    }

    private void ProcessScaling()
    {
        if (ManipulatedObject == null || !ShouldProcessScaling()) return;
        
        float scaleMultiplier = 1 + ScaleSensitivity * GestureManager.Instance.NavigationPosition.x;
        ManipulatedObject.transform.localScale *= scaleMultiplier;
    }

    private bool ShouldProcessRotation()
    {
        return GestureManager.Instance.IsNavigating && 
               ActionManager.Instance.CurrentAction == ActionManager.ActionType.Rotation;
    }

    private bool ShouldProcessScaling()
    {
        return GestureManager.Instance.IsNavigating && 
               ActionManager.Instance.CurrentAction == ActionManager.ActionType.Zoom;
    }

    private void ResetObjectTransform()
    {
        if (ManipulatedObject == null) return;

        ManipulatedObject.transform.position = Vector3.zero;
        ManipulatedObject.transform.rotation = Quaternion.identity;
        ManipulatedObject.transform.localScale = Vector3.one;
        isFlipped = false;
    }

    private void FlipObjectVertically()
    {
        if (ManipulatedObject == null) return;

        isFlipped = !isFlipped;
        float flipRotation = isFlipped ? 180f : 0f;
        
        ManipulatedObject.transform.rotation = Quaternion.Euler(flipRotation, 
            ManipulatedObject.transform.rotation.eulerAngles.y, 
            ManipulatedObject.transform.rotation.eulerAngles.z);
    }
}
