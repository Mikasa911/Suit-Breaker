using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LetterboxCamera : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();

        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect = cam.rect;

        if (scaleHeight < 1.0f)
        {
            // Pillarbox
            rect.width = scaleHeight;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleHeight) / 2.0f;
            rect.y = 0;
        }
        else
        {
            // Letterbox
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = 1.0f;
            rect.height = scaleWidth;
            rect.x = 0;
            rect.y = (1.0f - scaleWidth) / 2.0f;
        }

        cam.rect = rect;
    }
}
