using UnityEngine;
using System.Collections.Generic;
using System;

public class BeiDouSatelliteSystem : MonoBehaviour
{
    [Header("BeiDou Configuration")]
    public bool useBeiDouOnly = true;
    public float positionAccuracy = 2.5f; // meters
    public float updateInterval = 1.0f; // seconds

    [Header("Status")]
    public bool isActive = false;
    public int connectedSatellites = 0;
    public Vector2 currentPosition;
    public float currentAltitude;
    public float signalStrength = 0.8f;

    // BeiDou specific satellites (simplified simulation)
    private List<BeiDouSatellite> satellites = new List<BeiDouSatellite>();
    private float lastUpdateTime;

    public static BeiDouSatelliteSystem Instance { get; private set; }

    public event Action<Vector2> OnPositionUpdated;
    public event Action<int> OnSatelliteCountChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBeiDouSatellites();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBeiDouSatellites()
    {
        // Initialize BeiDou constellation (simplified) - Iran region
        satellites.Add(new BeiDouSatellite("BeiDou-1", 35.68f, 51.39f, 0.9f));
        satellites.Add(new BeiDouSatellite("BeiDou-2", 35.70f, 51.41f, 0.8f));
        satellites.Add(new BeiDouSatellite("BeiDou-3", 35.66f, 51.37f, 0.85f));
        satellites.Add(new BeiDouSatellite("BeiDou-4", 35.72f, 51.43f, 0.7f));
        satellites.Add(new BeiDouSatellite("BeiDou-5", 35.64f, 51.35f, 0.75f));

        connectedSatellites = satellites.Count;
        isActive = true;

        // Start at Tehran coordinates as default
        currentPosition = new Vector2(35.6892f, 51.3890f);
        currentAltitude = 1200.0f; // Tehran altitude
    }

    private void Update()
    {
        if (!isActive) return;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdatePosition();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdatePosition()
    {
        // Simulate BeiDou positioning with small random variations
        Vector2 offset = new Vector2(
            UnityEngine.Random.Range(-0.001f, 0.001f),
            UnityEngine.Random.Range(-0.001f, 0.001f)
        );

        currentPosition += offset;
        OnPositionUpdated?.Invoke(currentPosition);

        // Simulate signal strength variations
        signalStrength = Mathf.Clamp01(signalStrength + UnityEngine.Random.Range(-0.1f, 0.1f));

        // Simulate satellite count changes
        int newSatCount = UnityEngine.Random.Range(4, 8);
        if (newSatCount != connectedSatellites)
        {
            connectedSatellites = newSatCount;
            OnSatelliteCountChanged?.Invoke(connectedSatellites);
        }
    }

    public void SetDestination(Vector2 destination)
    {
        NavigationManager.Instance?.SetDestination(destination);
    }

    public float GetDistanceToDestination()
    {
        return NavigationManager.Instance?.GetDistanceToDestination() ?? 0f;
    }
}

[System.Serializable]
public class BeiDouSatellite
{
    public string name;
    public float latitude;
    public float longitude;
    public float signalStrength;

    public BeiDouSatellite(string name, float lat, float lon, float strength)
    {
        this.name = name;
        this.latitude = lat;
        this.longitude = lon;
        this.signalStrength = strength;
    }
}