using UnityEngine;
using UnityEngine.UI;

namespace CVAssistant.CameraImageProcessing
{
    public class ImageResizer
    {
        public enum AdjustMode
        {
            ToMinimum,
            ToMaximum
        }

        public static void AdjustImageToTexture(RawImage image, Vector2 target, AdjustMode adjustMode)
        {
            var ratio = target.x / target.y;
            var displaySize = image.transform.parent.GetComponent<RectTransform>().sizeDelta;
            if (displaySize.x - target.x > displaySize.y - target.y && adjustMode == AdjustMode.ToMinimum 
                || displaySize.x - target.x < displaySize.y - target.y && adjustMode == AdjustMode.ToMaximum)
            {
                image.rectTransform.sizeDelta = new Vector2(displaySize.x, displaySize.x / ratio);
            }
            else
            {
                image.rectTransform.sizeDelta = new Vector2(displaySize.y * ratio, displaySize.y);
            }
        }
    }
}
