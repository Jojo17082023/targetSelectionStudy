using UnityEngine.UI;
using UnityEngine;

public class ScrollController : MonoBehaviour
{
    public ScrollRect scrollRect; // Reference to the ScrollRect component

    // Scroll speed or amount
    public float scrollSpeed = 0.1f; // Adjust this value as needed

    public void ScrollUp()
    {
        // Scroll content upwards
        if (scrollRect.verticalNormalizedPosition < 1f)
        {
            scrollRect.verticalNormalizedPosition += scrollSpeed;
        }
    }

    public void ScrollDown()
    {
        // Scroll content downwards
        if (scrollRect.verticalNormalizedPosition > 0f)
        {
            scrollRect.verticalNormalizedPosition -= scrollSpeed;
        }
    }
}
