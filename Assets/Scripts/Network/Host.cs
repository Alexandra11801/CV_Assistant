using CVAssistant.CameraImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Rect = OpenCvSharp.Rect;

namespace CVAssistant.Network
{
    public class Host 
    {
        private static Host instance;
        private IPAddress address;

        private int imagePort = 13000;
        private TcpListener imageListener;
        private TcpClient imageClient;
        private NetworkStream imageStream;

        private int selectionPort = 12000;
        private TcpListener selectionListener;
        private TcpClient selectionClient;
        private NetworkStream selectionStream;

        private CVCameraImageProcessor imageProcessor;
        private List<Rect> selectRects;

        public IPAddress Address => address;
        public List<Rect> SelectRects => selectRects;

        private Host(RawImage image)
        {
            imageProcessor = image.GetComponent<CVCameraImageProcessor>();
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            address = hostAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            imageListener = new TcpListener(address, imagePort);
            selectionListener = new TcpListener(address, selectionPort);
            selectRects = new List<Rect>();
        }

        public static Host GetInstance(RawImage image)
        {
            if (instance == null)
            {
                instance = new Host(image);
            }
            return instance;
        }

        public async void StartListening()
        {
            imageListener.Start();
            selectionListener.Start();
            while (true)
            {
                var imageListenerTask = ListenForImageRequest();
                var selectionListenerTask = ListenForSelectionRequest();
                await Task.WhenAll(imageListenerTask, selectionListenerTask);
                WriteImageLoop();
                ReceiveSelectionLoop();
            }
        }

        public async Task ListenForImageRequest()
        {
            imageClient = await imageListener.AcceptTcpClientAsync();
            imageStream = imageClient.GetStream();
        }

        public async Task ListenForSelectionRequest()
        {
            selectionClient = await selectionListener.AcceptTcpClientAsync();
            selectionStream = selectionClient.GetStream();
        }

        private async void WriteImageLoop()
        {
            while (imageClient.Connected)
            {
                var texture = imageProcessor.ClearTexture;
                await SendTexture(texture);
            }
        }

        private async void ReceiveSelectionLoop()
        {
            while (selectionClient.Connected)
            {
                try
                {
                    await ReceiveSelection();
                }
                catch (Exception ex)
                {
                    selectRects.Clear();
                }
            }
        }

        private async Task SendTexture(Texture2D texture)
        {
            var bytes = ImageConversion.EncodeToJPG(texture, 25);
            await WriteInt(imageStream, texture.width);
            await WriteInt(imageStream, texture.height);
            var bytesCoded = Convert.ToBase64String(bytes);
            var bytesCodedArray = Encoding.UTF8.GetBytes(bytesCoded);
            await WriteInt(imageStream, bytesCodedArray.Length);
            await imageStream.WriteAsync(bytesCodedArray, 0, bytesCodedArray.Length);
        }

        private async Task ReceiveSelection()
        {
            var rectCount = await ReadInt(selectionStream);
            var newRects = new List<Rect>();
            for(int i = 0; i < rectCount; i++)
            {
                var x = await ReadInt(selectionStream);
                var y = await ReadInt(selectionStream);
                var width = await ReadInt(selectionStream);
                var height = await ReadInt(selectionStream);
                var selectRect = new Rect(x, y, width, height);
                newRects.Add(selectRect);
            }
            selectRects = newRects;
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