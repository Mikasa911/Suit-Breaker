using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    // Fixed aspect ratio: 9:16 (portrait)
    private readonly float targetAspect = 9f / 16f;

    private int lastWidth;
    private int lastHeight;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        UpdateAspect();
    }

    void Update()
    {
        // Detect window resize
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            UpdateAspect();
        }
    }

    void UpdateAspect()
    {
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // Add letterbox (black bars top/bottom)
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            // Add pillarbox (black bars left/right)
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}
