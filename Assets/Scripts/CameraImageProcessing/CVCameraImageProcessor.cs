using CVAssistant.Network;
using OpenCvSharp;
using OpenCvSharp.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace CVAssistant.CameraImageProcessing
{
    public class CVCameraImageProcessor : WebCamera
    {
        private Texture2D clearTexture;

        public Texture2D ClearTexture => clearTexture;

        protected override void Awake()
        {
            if (WebCamTexture.devices.Length > 0)
            {
                DeviceName = WebCamTexture.devices[0].name;
            }
        }

        protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
        {
            var image = OpenCvSharp.Unity.TextureToMat(input, TextureParameters);
            clearTexture = OpenCvSharp.Unity.MatToTexture(image, clearTexture);
            var rects = Host.GetInstance().SelectRects;
            foreach (var rect in rects)
            {
                Cv2.Rectangle(image, rect, Scalar.Red, 3);
            }
            output = OpenCvSharp.Unity.MatToTexture(image, output);
            ImageResizer.AdjustImageToTexture(Surface.GetComponent<RawImage>(), new Vector2(Screen.width, Screen.height), ImageResizer.AdjustMode.ToMinimum);
            return true;
        }
    }
}