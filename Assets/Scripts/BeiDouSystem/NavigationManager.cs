using UnityEngine;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    [Header("Navigation Settings")]
    public Vector2 destination;
    public bool hasDestination = false;
    public float routeRecalculationDistance = 50f;

    [Header("Route Information")]
    public List<Vector2> routePoints = new List<Vector2>();
    public float totalDistance;
    public float remainingDistance;
    public int estimatedTimeMinutes;

    public static NavigationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (BeiDouSatelliteSystem.Instance != null)
        {
            BeiDouSatelliteSystem.Instance.OnPositionUpdated += OnPositionUpdated;
        }
    }

    private void OnPositionUpdated(Vector2 currentPosition)
    {
        if (hasDestination)
        {
            CalculateRoute(currentPosition, destination);
            UpdateNavigationInstructions();
        }
    }

    public void SetDestination(Vector2 dest)
    {
        destination = dest;
        hasDestination = true;

        if (BeiDouSatelliteSystem.Instance != null)
        {
            Vector2 currentPos = BeiDouSatelliteSystem.Instance.currentPosition;
            CalculateRoute(currentPos, destination);
        }
    }

    private void CalculateRoute(Vector2 start, Vector2 end)
    {
        // Simple route calculation (in real app, use routing API)
        routePoints.Clear();
        routePoints.Add(start);

        // Add some intermediate points for more realistic route
        Vector2 midPoint = Vector2.Lerp(start, end, 0.5f);
        routePoints.Add(midPoint);
        routePoints.Add(end);

        // Calculate distances
        totalDistance = CalculateTotalDistance();
        remainingDistance = CalculateRemainingDistance(start);
        estimatedTimeMinutes = Mathf.RoundToInt(remainingDistance / 50f * 60f); // Assume 50 km/h average speed
    }

    private float CalculateTotalDistance()
    {
        float distance = 0f;
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            distance += Vector2.Distance(routePoints[i], routePoints[i + 1]) * 111.32f; // Convert to km
        }
        return distance;
    }

    private float CalculateRemainingDistance(Vector2 currentPosition)
    {
        if (routePoints.Count == 0) return 0f;

        float distance = Vector2.Distance(currentPosition, routePoints[0]) * 111.32f;
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            distance += Vector2.Distance(routePoints[i], routePoints[i + 1]) * 111.32f;
        }
        return distance;
    }

    public float GetDistanceToDestination()
    {
        if (!hasDestination || BeiDouSatelliteSystem.Instance == null) return 0f;

        Vector2 currentPos = BeiDouSatelliteSystem.Instance.currentPosition;
        return Vector2.Distance(currentPos, destination) * 111.32f; // Convert to km
    }

    private void UpdateNavigationInstructions()
    {
        NavigationUI.Instance?.UpdateNavigationInfo();
    }

    public void ClearRoute()
    {
        hasDestination = false;
        routePoints.Clear();
        totalDistance = 0f;
        remainingDistance = 0f;
        estimatedTimeMinutes = 0;
    }
}