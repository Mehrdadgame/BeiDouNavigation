using UnityEngine;

public class BeiDouNavigationApp : MonoBehaviour
{
    [Header("App Settings")]
    public string appVersion = "1.0.0";
    public bool autoStartBeiDou = true;

    [Header("Components")]
    public BeiDouSatelliteSystem satelliteSystem;
    public NavigationManager navigationManager;
    public MapDisplay mapDisplay;
    public NavigationUI navigationUI;

    private void Start()
    {
        InitializeApp();
    }

    private void InitializeApp()
    {
        Debug.Log($"BeiDou Navigation App v{appVersion} Starting...");

        // Initialize components if not already done
        if (autoStartBeiDou && BeiDouSatelliteSystem.Instance != null)
        {
            Debug.Log("BeiDou Satellite System Initialized");
            Debug.Log($"Connected to {BeiDouSatelliteSystem.Instance.connectedSatellites} BeiDou satellites");
        }

        // Set up input handling
        SetupInputHandling();

        Debug.Log("BeiDou Navigation App Ready");
    }

    private void SetupInputHandling()
    {
        // Handle touch input for map interaction
        // This would be expanded for mobile touch controls
    }

    private void Update()
    {
        // Handle keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Quick test: set destination to Isfahan
            Vector2 isfahan = new Vector2(32.6546f, 51.6680f);
            NavigationManager.Instance?.SetDestination(isfahan);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            NavigationManager.Instance?.ClearRoute();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            // Toggle follow user
            MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
            mapDisplay?.ToggleFollowUser();
        }

        // Handle mouse click for setting destination
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
            if (mapDisplay != null)
            {
                mapDisplay.SetDestinationAtScreenPoint(Input.mousePosition);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (BeiDouSatelliteSystem.Instance != null)
        {
            BeiDouSatelliteSystem.Instance.isActive = !pauseStatus;
        }
    }
}