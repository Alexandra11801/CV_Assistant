using CVAssistant.CameraImageProcessing;
using CVAssistant.ObjectsTracking;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        private int imagePort = 13000;
        private TcpClient imageClient;
        private NetworkStream imageStream;

        private int selectionPort = 12000;
        private TcpClient selectionClient;
        private NetworkStream selectionStream;

        private RawImage image;
        private Texture2D receivedTexture;
        private ObjectsTracker tracker;

        public string Address => address;
        public Texture2D ReceivedTexture => receivedTexture;

        private Assistant(RawImage image)
        {
            this.image = image;
            tracker = image.GetComponent<ObjectsTracker>();
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
            var imageConnectionTask = RequestImage(address);
            var selectionConnectionTask = RequestSelectionSending(address);
            await Task.WhenAll(imageConnectionTask, selectionConnectionTask);
            ReadImageLoop();
        }

        public async Task RequestImage(string address)
        {
            imageClient = new TcpClient();
            await imageClient.ConnectAsync(IPAddress.Parse(address), imagePort);
            imageStream = imageClient.GetStream();
        }

        public async Task RequestSelectionSending(string address)
        {
            selectionClient = new TcpClient();
            await selectionClient.ConnectAsync(IPAddress.Parse(address), selectionPort); 
            selectionStream = selectionClient.GetStream();
        }

        public void Disconnect()
        {
            imageClient.Close();
            selectionClient.Close();
        }

        private async void ReadImageLoop()
        {
            while (imageClient.Connected)
            {
                try
                {
                   await LoadScreenImage();
                }
                catch(Exception ex)
                {
                    Disconnect();
                }
            }
        }

        private async void SendSelectionLoop()
        {
            while (selectionClient.Connected)
            {
                try
                {
                    await SendSelection();
                }
                catch (Exception ex)
                {
                    Disconnect();
                }
            }
        }

        private async Task LoadScreenImage()
        {
            var width = await ReadInt(imageStream);
            var height = await ReadInt(imageStream);
            var bytesCodedArrayLength = await ReadInt(imageStream);
            var bytesCodedArray = new byte[bytesCodedArrayLength];
            var bytesCount = 0;
            while (bytesCount < bytesCodedArrayLength)
            {
                bytesCount += await imageStream.ReadAsync(bytesCodedArray, bytesCount, bytesCodedArrayLength - bytesCount);
            }
            var bytesCoded = Encoding.UTF8.GetString(bytesCodedArray);
            var bytes = Convert.FromBase64String(bytesCoded);
            var texture = new Texture2D(width, height);
            texture.LoadImage(bytes);
            ImageResizer.AdjustImageToTexture(image, new Vector2(width, height), ImageResizer.AdjustMode.ToMinimum);
            receivedTexture = texture;
            var rects = tracker.CurrentRects;
            var mat = OpenCvSharp.Unity.TextureToMat(texture);
            foreach (var rect in rects)
            {
                Cv2.Rectangle(mat, rect, Scalar.Red, 3);
            }
            image.texture = OpenCvSharp.Unity.MatToTexture(mat);
        }

        public async Task SendSelection()
        {
            var selectRects = tracker.CurrentRects;
            await WriteInt(selectionStream, selectRects.Count);
            foreach (var selectRect in selectRects)
            {
                await WriteInt(selectionStream, selectRect.Location.X);
                await WriteInt(selectionStream, selectRect.Location.Y);
                await WriteInt(selectionStream, selectRect.Size.Width);
                await WriteInt(selectionStream, selectRect.Size.Height);
            }
        }

        private async Task<int> ReadInt(NetworkStream stream)
        {
            var bytes = new byte[4];
            var bytesCount = 0;
            while (bytesCount < 4)
            {
                bytesCount += await stream.ReadAsync(bytes, bytesCount, 4 - bytesCount);
            }
            return BitConverter.ToInt32(bytes);
        }

        private async Task WriteInt(NetworkStream stream, int value)
        {
            await stream.WriteAsync(BitConverter.GetBytes((uint)value), 0, 4);
        }
    }
}