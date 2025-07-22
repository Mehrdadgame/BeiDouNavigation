using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapDisplay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Map Components")]
    public Transform mapContainer; // والد برای کاشی‌های نقشه
    public Transform userMarker; // نشانگر موقعیت کاربر
    public Transform destinationMarker; // نشانگر مقصد
    public LineRenderer routeLineRenderer; // خط مسیر
    public OSMTileLoader tileLoader; // ارجاع به OSMTileLoader برای بارگذاری کاشی‌های OSM

    [Header("Map Settings")]
    public float zoomLevel = 15f; // سطح زوم (هماهنگ با OSMTileLoader)
    public Vector2 mapCenter; // مرکز نقشه (lat, lon)
    public float mapScale = 0.001f; // مقیاس برای تبدیل مختصات به متر
    public float dragSensitivity = 0.01f; // حساسیت کشیدن نقشه
    public float zoomSensitivity = 0.5f; // حساسیت زوم با تاچ یا ماوس
    public float minZoomLevel = 5f; // حداقل سطح زوم
    public float maxZoomLevel = 19f; // حداکثر سطح زوم
    public float minOrthographicSize = 0.05f; // حداقل اندازه دوربین
    public float maxOrthographicSize = 5f; // حداکثر اندازه دوربین

    [Header("Camera Follow")]
    public bool followUser = true; // دنبال کردن خودکار کاربر
    public float followSpeed = 5f; // سرعت دنبال کردن
    public float followThreshold = 0.01f; // آستانه حرکت دوربین

    private Camera mapCamera;
    private Vector2 lastPosition;
    private bool isDragging = false;
    private Vector2 lastDragPosition;
    private bool isTouchDragging = false;
    private Vector2 lastTouchPosition;

    private void Start()
    {
        mapCamera = Camera.main;
        InitializeMap();

        if (BeiDouSatelliteSystem.Instance != null)
            BeiDouSatelliteSystem.Instance.OnPositionUpdated += OnPositionUpdated;
    }

    private void InitializeMap()
    {
        // تنظیم مرکز نقشه به موقعیت فعلی یا تهران به صورت پیش‌فرض
        mapCenter = BeiDouSatelliteSystem.Instance?.currentPosition ?? new Vector2(35.6892f, 51.3890f);
        UpdateMapView();

        // موقعیت اولیه دوربین
        if (mapCamera != null)
        {
            Vector3 iranPosition = LatLonToWorldPosition(mapCenter);
            mapCamera.transform.position = new Vector3(iranPosition.x, iranPosition.y, -10f);
            mapCamera.orthographicSize = 0.5f; // اندازه اولیه دوربین
        }
    }

    private void OnPositionUpdated(Vector2 newPosition)
    {
        if (Vector2.Distance(lastPosition, Vector2.zero) < 0.001f)
        {
            Vector3 targetWorldPos = LatLonToWorldPosition(newPosition);
            if (mapCamera != null)
            {
                mapCamera.transform.position = new Vector3(targetWorldPos.x, targetWorldPos.y, -10f);
                mapCenter = newPosition;
                UpdateMapView();
            }
            Debug.Log($"First location found! Camera moved to: {newPosition}");
        }

        lastPosition = newPosition;
        UpdateUserMarker(newPosition);

        if (followUser && !isDragging)
            UpdateCameraFollow(newPosition);

        UpdateRouteDisplay();
    }

    private void UpdateCameraFollow(Vector2 userPosition)
    {
        if (mapCamera == null) return;

        Vector3 targetWorldPos = LatLonToWorldPosition(userPosition);
        Vector3 currentCameraPos = mapCamera.transform.position;

        float distance = Vector2.Distance(new Vector2(currentCameraPos.x, currentCameraPos.y),
                                         new Vector2(targetWorldPos.x, targetWorldPos.y));

        if (distance > followThreshold)
        {
            Vector3 newCameraPos = Vector3.Lerp(currentCameraPos,
                                               new Vector3(targetWorldPos.x, targetWorldPos.y, currentCameraPos.z),
                                               Time.deltaTime * followSpeed);
            mapCamera.transform.position = newCameraPos;
            mapCenter = WorldPositionToLatLon(newCameraPos);
            UpdateMapView();
        }
    }

    private void UpdateUserMarker(Vector2 position)
    {
        if (userMarker != null)
            userMarker.position = LatLonToWorldPosition(position);
    }

    private void UpdateMapView()
    {
        if (tileLoader != null)
        {
            tileLoader.zoomLevel = (int)zoomLevel;
            tileLoader.UpdateTiles(mapCenter);
        }
    }

    private void UpdateRouteDisplay()
    {
        if (NavigationManager.Instance != null && NavigationManager.Instance.hasDestination)
        {
            destinationMarker.gameObject.SetActive(true);
            Vector3 destWorldPos = LatLonToWorldPosition(NavigationManager.Instance.destination);
            destinationMarker.position = destWorldPos;
            DrawRoute();
        }
        else
        {
            destinationMarker.gameObject.SetActive(false);
            if (routeLineRenderer != null)
                routeLineRenderer.positionCount = 0;
        }
    }

    private void DrawRoute()
    {
        if (routeLineRenderer == null || NavigationManager.Instance == null) return;

        var routePoints = NavigationManager.Instance.routePoints;
        routeLineRenderer.positionCount = routePoints.Count;

        for (int i = 0; i < routePoints.Count; i++)
            routeLineRenderer.SetPosition(i, LatLonToWorldPosition(routePoints[i]));
    }

    private Vector3 LatLonToWorldPosition(Vector2 latLon)
    {
        float x = (latLon.y - mapCenter.y) * mapScale;
        float y = (latLon.x - mapCenter.x) * mapScale;
        return new Vector3(x, y, 0);
    }

    private Vector2 WorldPositionToLatLon(Vector3 worldPos)
    {
        float lon = (worldPos.x / mapScale) + mapCenter.y;
        float lat = (worldPos.y / mapScale) + mapCenter.x;
        return new Vector2(lat, lon);
    }

    private void Update()
    {
        HandleMobileInput();
        HandleMouseScroll();
    }

    private void HandleMobileInput()
    {
        // زوم با دو انگشت (Pinch-to-Zoom)
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            if (mapCamera != null)
            {
                float newOrthoSize = mapCamera.orthographicSize + deltaMagnitudeDiff * zoomSensitivity * Time.deltaTime;
                newOrthoSize = Mathf.Clamp(newOrthoSize, minOrthographicSize, maxOrthographicSize);
                mapCamera.orthographicSize = newOrthoSize;

                zoomLevel = Mathf.Lerp(maxZoomLevel, minZoomLevel, (newOrthoSize - minOrthographicSize) / (maxOrthographicSize - minOrthographicSize));
                zoomLevel = Mathf.Clamp(zoomLevel, minZoomLevel, maxZoomLevel);

                if (tileLoader != null)
                {
                    tileLoader.zoomLevel = (int)zoomLevel;
                    tileLoader.UpdateTiles(mapCenter);
                }
            }

            isDragging = false;
            isTouchDragging = false;
        }
        // کشیدن نقشه با یک انگشت
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isTouchDragging = true;
                    isDragging = true;
                    followUser = false;
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (isTouchDragging)
                    {
                        Vector2 deltaMove = (touch.position - lastTouchPosition) * dragSensitivity;
                        lastTouchPosition = touch.position;

                        if (mapCamera != null)
                        {
                            Vector3 worldDelta = mapCamera.ScreenToWorldPoint(new Vector3(deltaMove.x, deltaMove.y, 0)) -
                                                mapCamera.ScreenToWorldPoint(Vector3.zero);
                            worldDelta.z = 0;
                            mapCamera.transform.position -= worldDelta;
                            mapCenter = WorldPositionToLatLon(mapCamera.transform.position);
                            UpdateMapView();
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isTouchDragging = false;
                    isDragging = false;
                    break;
            }
        }

        // ورودی ماوس برای تست دسکتاپ
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            followUser = false;
            lastDragPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 deltaMove = (currentMousePos - lastDragPosition) * dragSensitivity;
            lastDragPosition = currentMousePos;

            if (mapCamera != null)
            {
                Vector3 worldDelta = mapCamera.ScreenToWorldPoint(new Vector3(deltaMove.x, deltaMove.y, 0)) -
                                    mapCamera.ScreenToWorldPoint(Vector3.zero);
                worldDelta.z = 0;
                mapCamera.transform.position -= worldDelta;
                mapCenter = WorldPositionToLatLon(mapCamera.transform.position);
                UpdateMapView();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void HandleMouseScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && mapCamera != null)
        {
            float newOrthoSize = mapCamera.orthographicSize - scroll * zoomSensitivity * mapCamera.orthographicSize;
            newOrthoSize = Mathf.Clamp(newOrthoSize, minOrthographicSize, maxOrthographicSize);
            mapCamera.orthographicSize = newOrthoSize;

            zoomLevel = Mathf.Lerp(maxZoomLevel, minZoomLevel, (newOrthoSize - minOrthographicSize) / (maxOrthographicSize - minOrthographicSize));
            zoomLevel = Mathf.Clamp(zoomLevel, minZoomLevel, maxZoomLevel);

            if (tileLoader != null)
            {
                tileLoader.zoomLevel = (int)zoomLevel;
                tileLoader.UpdateTiles(mapCenter);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData) { }
    public void OnPointerUp(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }

    public void SetDestination(Vector2 destination)
    {
        BeiDouSatelliteSystem.Instance?.SetDestination(destination);
    }

    public void SetDestinationAtScreenPoint(Vector2 screenPoint)
    {
        Vector3 worldPoint = mapCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0));
        Vector2 latLon = WorldPositionToLatLon(worldPoint);
        SetDestination(latLon);
    }

    public void ToggleFollowUser()
    {
        followUser = !followUser;
        if (followUser && BeiDouSatelliteSystem.Instance != null)
            UpdateCameraFollow(BeiDouSatelliteSystem.Instance.currentPosition);
    }

    public void ZoomIn()
    {
        if (mapCamera != null)
        {
            mapCamera.orthographicSize = Mathf.Max(mapCamera.orthographicSize * 0.8f, minOrthographicSize);
            zoomLevel = Mathf.Min(zoomLevel + 1, maxZoomLevel);
            if (tileLoader != null)
            {
                tileLoader.zoomLevel = (int)zoomLevel;
                tileLoader.UpdateTiles(mapCenter);
            }
        }
    }

    public void ZoomOut()
    {
        if (mapCamera != null)
        {
            mapCamera.orthographicSize = Mathf.Min(mapCamera.orthographicSize * 1.2f, maxOrthographicSize);
            zoomLevel = Mathf.Max(zoomLevel - 1, minZoomLevel);
            if (tileLoader != null)
            {
                tileLoader.zoomLevel = (int)zoomLevel;
                tileLoader.UpdateTiles(mapCenter);
            }
        }
    }
}