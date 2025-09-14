using UnityEngine;

public class GestureManager : Singleton<GestureManager>
{
    public UnityEngine.XR.WSA.Input.GestureRecognizer NavigationRecognizer { get; private set; }
    public UnityEngine.XR.WSA.Input.GestureRecognizer ManipulationRecognizer { get; private set; }
    public UnityEngine.XR.WSA.Input.GestureRecognizer ActiveRecognizer { get; private set; }

    public bool IsNavigating { get; private set; }
    public bool IsManipulating { get; private set; }
    public Vector3 NavigationPosition { get; private set; }
    public Vector3 ManipulationPosition { get; private set; }

    void Awake()
    {
        InitializeNavigationRecognizer();
        InitializeManipulationRecognizer();
        SetActiveRecognizer(ManipulationRecognizer);
    }

    private void InitializeNavigationRecognizer()
    {
        NavigationRecognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
        NavigationRecognizer.SetRecognizableGestures(UnityEngine.XR.WSA.Input.GestureSettings.NavigationX);
        
        NavigationRecognizer.NavigationStartedEvent += OnNavigationStarted;
        NavigationRecognizer.NavigationUpdatedEvent += OnNavigationUpdated;
        NavigationRecognizer.NavigationCompletedEvent += OnNavigationCompleted;
        NavigationRecognizer.NavigationCanceledEvent += OnNavigationCanceled;
    }

    private void InitializeManipulationRecognizer()
    {
        ManipulationRecognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
        ManipulationRecognizer.SetRecognizableGestures(UnityEngine.XR.WSA.Input.GestureSettings.ManipulationTranslate);

        ManipulationRecognizer.ManipulationStartedEvent += OnManipulationStarted;
        ManipulationRecognizer.ManipulationUpdatedEvent += OnManipulationUpdated;
        ManipulationRecognizer.ManipulationCompletedEvent += OnManipulationCompleted;
        ManipulationRecognizer.ManipulationCanceledEvent += OnManipulationCanceled;
    }

    void OnDestroy()
    {
        UnsubscribeNavigationEvents();
        UnsubscribeManipulationEvents();
    }

    private void UnsubscribeNavigationEvents()
    {
        if (NavigationRecognizer != null)
        {
            NavigationRecognizer.NavigationStartedEvent -= OnNavigationStarted;
            NavigationRecognizer.NavigationUpdatedEvent -= OnNavigationUpdated;
            NavigationRecognizer.NavigationCompletedEvent -= OnNavigationCompleted;
            NavigationRecognizer.NavigationCanceledEvent -= OnNavigationCanceled;
        }
    }

    private void UnsubscribeManipulationEvents()
    {
        if (ManipulationRecognizer != null)
        {
            ManipulationRecognizer.ManipulationStartedEvent -= OnManipulationStarted;
            ManipulationRecognizer.ManipulationUpdatedEvent -= OnManipulationUpdated;
            ManipulationRecognizer.ManipulationCompletedEvent -= OnManipulationCompleted;
            ManipulationRecognizer.ManipulationCanceledEvent -= OnManipulationCanceled;
        }
    }

    public void SetActiveRecognizer(UnityEngine.XR.WSA.Input.GestureRecognizer newRecognizer)
    {
        if (newRecognizer == null || ActiveRecognizer == newRecognizer) return;

        if (ActiveRecognizer != null)
        {
            ActiveRecognizer.CancelGestures();
            ActiveRecognizer.StopCapturingGestures();
        }

        newRecognizer.StartCapturingGestures();
        ActiveRecognizer = newRecognizer;
    }

    private void OnNavigationStarted(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 relativePosition, Ray ray)
    {
        IsNavigating = true;
        NavigationPosition = relativePosition;
    }

    private void OnNavigationUpdated(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 relativePosition, Ray ray)
    {
        IsNavigating = true;
        NavigationPosition = relativePosition;
    }

    private void OnNavigationCompleted(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 relativePosition, Ray ray)
    {
        IsNavigating = false;
    }

    private void OnNavigationCanceled(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 relativePosition, Ray ray)
    {
        IsNavigating = false;
    }

    private void OnManipulationStarted(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 position, Ray ray)
    {
        IsManipulating = true;
        ManipulationPosition = position;
    }

    private void OnManipulationUpdated(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 position, Ray ray)
    {
        IsManipulating = true;
        ManipulationPosition = position;
    }

    private void OnManipulationCompleted(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 position, Ray ray)
    {
        IsManipulating = false;
    }

    private void OnManipulationCanceled(UnityEngine.XR.WSA.Input.InteractionSourceKind source, Vector3 position, Ray ray)
    {
        IsManipulating = false;
    }
}
