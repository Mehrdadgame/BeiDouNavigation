using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI satelliteCountText;
    public TextMeshProUGUI signalStrengthText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI estimatedTimeText;
    public TextMeshProUGUI speedText;
    public Slider signalStrengthSlider;
    public Button startNavigationButton;
    public Button stopNavigationButton;
    public TMP_InputField destinationInput;

    [Header("Status Indicators")]
    public Image beidouStatusIcon;
    public Color connectedColor = Color.green;
    public Color disconnectedColor = Color.red;

    public static NavigationUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeUI();

        if (BeiDouSatelliteSystem.Instance != null)
        {
            BeiDouSatelliteSystem.Instance.OnPositionUpdated += OnPositionUpdated;
            BeiDouSatelliteSystem.Instance.OnSatelliteCountChanged += OnSatelliteCountChanged;
        }

        // Setup button listeners
        startNavigationButton.onClick.AddListener(StartNavigation);
        stopNavigationButton.onClick.AddListener(StopNavigation);

        stopNavigationButton.gameObject.SetActive(false);
    }

    private void InitializeUI()
    {
        UpdateBeiDouStatus();
        UpdateNavigationInfo();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (BeiDouSatelliteSystem.Instance != null)
        {
            var beidou = BeiDouSatelliteSystem.Instance;

            // Update position display
            positionText.text = $"位置: {beidou.currentPosition.x:F6}, {beidou.currentPosition.y:F6}";

            // Update satellite count
            satelliteCountText.text = $"BeiDou 卫星: {beidou.connectedSatellites}";

            // Update signal strength
            signalStrengthText.text = $"信号强度: {(beidou.signalStrength * 100):F0}%";
            signalStrengthSlider.value = beidou.signalStrength;

            // Calculate and display speed (simplified)
            float speed = UnityEngine.Random.Range(0f, 60f); // km/h
            speedText.text = $"速度: {speed:F1} km/h";

            // Update BeiDou status
            UpdateBeiDouStatus();
        }
    }

    private void OnPositionUpdated(Vector2 position)
    {
        // Position is updated in Update() method
    }

    private void OnSatelliteCountChanged(int count)
    {
        // Satellite count is updated in Update() method
    }

    public void UpdateNavigationInfo()
    {
        if (NavigationManager.Instance != null && NavigationManager.Instance.hasDestination)
        {
            var nav = NavigationManager.Instance;
            distanceText.text = $"剩余距离: {nav.remainingDistance:F1} km";
            estimatedTimeText.text = $"预计时间: {nav.estimatedTimeMinutes} 分钟";
        }
        else
        {
            distanceText.text = "剩余距离: --";
            estimatedTimeText.text = "预计时间: --";
        }
    }

    private void UpdateBeiDouStatus()
    {
        if (BeiDouSatelliteSystem.Instance != null && BeiDouSatelliteSystem.Instance.isActive)
        {
            beidouStatusIcon.color = connectedColor;
        }
        else
        {
            beidouStatusIcon.color = disconnectedColor;
        }
    }

    public void StartNavigation()
    {
        string destination = destinationInput.text;
        if (!string.IsNullOrEmpty(destination))
        {
            // Parse destination (simplified - in real app, use geocoding service)
            Vector2 destCoords = ParseDestination(destination);
            NavigationManager.Instance?.SetDestination(destCoords);

            startNavigationButton.gameObject.SetActive(false);
            stopNavigationButton.gameObject.SetActive(true);
        }
    }

    public void StopNavigation()
    {
        NavigationManager.Instance?.ClearRoute();
        startNavigationButton.gameObject.SetActive(true);
        stopNavigationButton.gameObject.SetActive(false);
    }

    private Vector2 ParseDestination(string destination)
    {
        // Parse destinations for Iran and surrounding regions
        string dest = destination.ToLower();

        if (dest.Contains("تهران") || dest.Contains("tehran"))
            return new Vector2(35.6892f, 51.3890f);
        else if (dest.Contains("اصفهان") || dest.Contains("isfahan"))
            return new Vector2(32.6546f, 51.6680f);
        else if (dest.Contains("مشهد") || dest.Contains("mashhad"))
            return new Vector2(36.2605f, 59.6168f);
        else if (dest.Contains("شیراز") || dest.Contains("shiraz"))
            return new Vector2(29.5918f, 52.5837f);
        else if (dest.Contains("تبریز") || dest.Contains("tabriz"))
            return new Vector2(38.0800f, 46.2919f);
        else if (dest.Contains("کرج") || dest.Contains("karaj"))
            return new Vector2(35.8327f, 50.9916f);
        else if (dest.Contains("قم") || dest.Contains("qom"))
            return new Vector2(34.6401f, 50.8764f);
        else if (dest.Contains("اهواز") || dest.Contains("ahvaz"))
            return new Vector2(31.3183f, 48.6706f);
        else if (dest.Contains("کرمانشاه") || dest.Contains("kermanshah"))
            return new Vector2(34.3277f, 47.0778f);
        else if (dest.Contains("رشت") || dest.Contains("rasht"))
            return new Vector2(37.2808f, 49.5832f);
        else
            return new Vector2(35.6892f, 51.3890f); // Default to Tehran
    }
}