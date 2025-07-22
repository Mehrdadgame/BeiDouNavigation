using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OSMTileLoader : MonoBehaviour
{
    [Header("Tile Settings")]
    public int zoomLevel = 15; // سطح زوم (5 تا 19)
    public Vector2 mapCenter = new Vector2(35.6892f, 51.3890f); // مختصات تهران
    public float tileSize = 256f; // اندازه کاشی در پیکسل
    public int tilesPerSide = 5; // شبکه 5x5 کاشی برای پوشش بیشتر
    public float scaleFactor = 0.001f; // مقیاس کوچکتر برای کاشی‌ها

    private Dictionary<Vector2Int, RawImage> tileImages = new Dictionary<Vector2Int, RawImage>();
    private string tileServerUrl = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
    private Camera mapCamera;

    private void Start()
    {
        mapCamera = Camera.main;
        UpdateTiles(mapCenter);
    }

    public void UpdateTiles(Vector2 newCenter)
    {
        mapCenter = newCenter;
        StartCoroutine(LoadTiles());
    }

    private IEnumerator LoadTiles()
    {
        Vector2Int centerTile = LatLonToTile(mapCenter, zoomLevel);
        int halfTiles = tilesPerSide / 2;

        // حذف کاشی‌های قدیمی
        foreach (var tile in tileImages)
        {
            Destroy(tile.Value.gameObject);
        }
        tileImages.Clear();

        // بارگذاری کاشی‌های جدید
        for (int x = centerTile.x - halfTiles; x <= centerTile.x + halfTiles; x++)
        {
            for (int y = centerTile.y - halfTiles; y <= centerTile.y + halfTiles; y++)
            {
                Vector2Int tileCoord = new Vector2Int(x, y);
                yield return StartCoroutine(DownloadTile(zoomLevel, x, y));
            }
        }
    }

    private IEnumerator DownloadTile(int zoom, int x, int y)
    {
        // اعتبارسنجی مختصات کاشی
        int maxTile = 1 << zoom;
        if (x < 0 || x >= maxTile || y < 0 || y >= maxTile)
        {
            Debug.LogWarning($"Tile coordinates {x},{y} out of bounds for zoom {zoom}");
            yield break;
        }

        string url = string.Format(tileServerUrl, zoom, x, y);
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            www.SetRequestHeader("User-Agent", "BeiDouNavigationApp/1.0 (Unity)");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                CreateTileObject(x, y, texture);
            }
            else
            {
                Debug.LogError($"Failed to load tile {x},{y}: {www.error}");
            }
        }
    }

    private void CreateTileObject(int x, int y, Texture2D texture)
    {
        GameObject tileObject = new GameObject($"Tile_{x}_{y}");
        tileObject.transform.SetParent(transform);
        RawImage tileImage = tileObject.AddComponent<RawImage>();
        tileImage.texture = texture;

        // محاسبه موقعیت کاشی در مختصات جهانی
        Vector2 tileCenterLatLon = TileToLatLon(new Vector2Int(x, y), zoomLevel);
        Vector3 worldPos = LatLonToWorldPosition(tileCenterLatLon);

        // تنظیم موقعیت و اندازه کاشی
        tileObject.transform.position = worldPos;
        tileImage.rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        float tileWorldSize = TileToWorldSize(zoomLevel);
        tileObject.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);

        tileImages[new Vector2Int(x, y)] = tileImage;
    }

    private Vector2Int LatLonToTile(Vector2 latLon, int zoom)
    {
        int n = 1 << zoom;
        int x = (int)((latLon.y + 180.0f) / 360.0f * n);
        double latRad = latLon.x * Mathf.Deg2Rad;
        int y = (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + (1.0 / System.Math.Cos(latRad))) / System.Math.PI) / 2.0 * n);
        return new Vector2Int(x, y);
    }

    private Vector2 TileToLatLon(Vector2Int tile, int zoom)
    {
        int n = 1 << zoom;
        float lon = (tile.x / (float)n) * 360.0f - 180.0f;
        double latRad = System.Math.Atan(System.Math.Sinh(System.Math.PI - (tile.y / (float)n) * 2.0 * System.Math.PI));
        float lat = (float)(latRad * 180.0 / System.Math.PI);
        return new Vector2(lat, lon);
    }

    private Vector3 LatLonToWorldPosition(Vector2 latLon)
    {
        float x = (latLon.y - mapCenter.y) * scaleFactor;
        float y = (latLon.x - mapCenter.x) * scaleFactor;
        return new Vector3(x, y, 0);
    }

    private float TileToWorldSize(int zoom)
    {
        // محاسبه اندازه کاشی در مختصات جهانی
        float earthCircumference = 40075016.68f; // محیط زمین در متر
        float tileCount = 1 << zoom; // تعداد کاشی‌ها در سطح زوم
        return (earthCircumference / tileCount) * scaleFactor / tileSize;
    }
}