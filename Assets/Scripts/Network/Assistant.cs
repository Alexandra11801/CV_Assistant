using CVAssistant.CameraImageProcessing;
using CVAssistant.ObjectsTracking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Rect = OpenCvSharp.Rect;

namespace CVAssistant.Network
{
    public class Assistant
    {
        private static Assistant instance;
        private string address;
        private TcpClient client;
        private NetworkStream stream;
        private RawImage image;
        private ObjectsTracker tracker;
        private List<Rect> selectRects;

        public string Address => address;

        private Assistant(RawImage image)
        {
            this.image = image;
            tracker = image.GetComponent<ObjectsTracker>();
            selectRects = new List<Rect>();
        }

        public static Assistant GetInstance(RawImage image)
        {
            if (instance == null)
            {
                instance = new Assistant(image);
            }
            return instance;
        }

        public async Task ConnectToHost(string address)
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(address), 13000);
            stream = client.GetStream();
            ReadStream();
        }

        public void Disconnect()
        {
            client.Close();
        }

        private async void ReadStream()
        {
            while (client.Connected)
            {
                try
                {
                    await LoadScreenImage();
                    await SendSelection();
                }
                catch(Exception ex)
                {
                    Disconnect();
                }
            }
        }

        private async Task LoadScreenImage()
        {
            var width = await ReadInt();
            var height = await ReadInt();
            var bytesCodedArrayLength = await ReadInt();
            var bytesCodedArray = new byte[bytesCodedArrayLength];
            var bytesCount = 0;
            while (bytesCount < bytesCodedArrayLength)
            {
                bytesCount += await stream.ReadAsync(bytesCodedArray, bytesCount, bytesCodedArrayLength - bytesCount);
            }
            var bytesCoded = Encoding.UTF8.GetString(bytesCodedArray);
            var bytes = Convert.FromBase64String(bytesCoded);
            var texture = new Texture2D(width, height);
            texture.LoadImage(bytes);
            ImageResizer.AdjustImageToTexture(image, new Vector2(width, height), ImageResizer.AdjustMode.ToMinimum);
            image.texture = texture;
            selectRects = tracker.RenderSelectRects();
        }

        private async Task SendSelection()
        {
            await stream.WriteAsync(BitConverter.GetBytes((uint)selectRects.Count), 0, 4);
            foreach (var selectRect in selectRects)
            {
                await stream.WriteAsync(BitConverter.GetBytes((uint)selectRect.Location.X), 0, 4);
                await stream.WriteAsync(BitConverter.GetBytes((uint)selectRect.Location.Y), 0, 4);
                await stream.WriteAsync(BitConverter.GetBytes((uint)selectRect.Size.Width), 0, 4);
                await stream.WriteAsync(BitConverter.GetBytes((uint)selectRect.Size.Height), 0, 4);
            }
        }

        private async Task<int> ReadInt()
        {
            var bytes = new byte[4];
            var bytesCount = 0;
            while (bytesCount < 4)
            {
                bytesCount += await stream.ReadAsync(bytes, bytesCount, 4 - bytesCount);
            }
            return BitConverter.ToInt32(bytes);
        }
    }
}