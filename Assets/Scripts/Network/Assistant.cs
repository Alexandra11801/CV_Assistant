using CVAssistant.CameraImageProcessing;
using CVAssistant.ObjectsTracking;
using OpenCvSharp;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CVAssistant.Network
{
    public class Assistant : SocketHandler
    {
        public struct ImageInfo
        {
            byte[] bytes;
            int width;
            int height;

            public byte[] Bytes => bytes;
            public int Width => width;
            public int Height => height;

            public ImageInfo(byte[] bytes, int width, int height)
            {
                this.bytes = bytes;
                this.width = width;
                this.height = height;
            }
        }

        private RawImage image;
        private Texture2D receivedTexture;
        private ObjectsTracker tracker;
        private GameObject startMenu;

        public Texture2D ReceivedTexture => receivedTexture;

        public GameObject StartMenu
        {
            get { return startMenu; }
            set { startMenu = value; }
        }

        public static Assistant GetInstance()
        {
            if (instance == null)
            {
                instance = new Assistant();
            }
            return (Assistant)instance;
        }

        public void SetImage(RawImage image)
        {
            this.image = image;
            tracker = image.GetComponent<ObjectsTracker>();
        }

        //Функция передачи аудио отключена, так как выбранные инструменты не удовлетворяют требованиям производительности. Планируется дальнейшая отладка и/или переработка функции.

        public async Task ConnectToHost(string address)
        {
            var imageConnectionTask = RequestImage(address);
            var selectionConnectionTask = RequestSelectionSending(address);
            //var audioConnectionTask = RequestAudio(address);
            await Task.WhenAll(imageConnectionTask, selectionConnectionTask);
            cancellationTokenSource = new CancellationTokenSource();
            ReadImageLoop();
            tracker.StartTracking();
            //audioSender.StartSendingAudio();
            //audioReceiver.StartPlay();
            //ReceiveAudioLoop();
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

        public async Task RequestAudio(string address)
        {
            audioClient = new TcpClient();
            await audioClient.ConnectAsync(IPAddress.Parse(address), audioPort);
            audioStream = audioClient.GetStream();
        }

        public override void Disconnect(Exception e)
        {
            base.Disconnect(e);
            image.texture = null;
            image.gameObject.SetActive(false);
            tracker.StopTracking();
            tracker.enabled = false;
            startMenu.SetActive(true);
        }

        private async void ReadImageLoop()
        {
            while (imageClient.Connected)
            {
                try
                {
                    var task = Task.Run(() => LoadScreenImage(cancellationTokenSource.Token));
                    var imageInfo = await task;
                    RenderImage(imageInfo);
                }
                catch(Exception ex)
                {
                    Disconnect(ex);
                }
            }
        }

        public async Task StartSendingSelection()
        {
            try
            {
                var task = Task.Run(() => SendSelection(cancellationTokenSource.Token));
                await task;
            }
            catch (Exception ex)
            {
                Disconnect(ex);
            }
        }

        private async Task<ImageInfo> LoadScreenImage(CancellationToken cancellationToken)
        {
            var width = await ReadInt(imageStream, cancellationToken);
            var height = await ReadInt(imageStream, cancellationToken);
            var bytesCodedArrayLength = await ReadInt(imageStream, cancellationToken);
            var bytesCodedArray = new byte[bytesCodedArrayLength];
            var bytesCount = 0;
            while (bytesCount < bytesCodedArrayLength)
            {
                bytesCount += await imageStream.ReadAsync(bytesCodedArray, bytesCount, bytesCodedArrayLength - bytesCount, cancellationToken);
            }
            var bytesCoded = Encoding.UTF8.GetString(bytesCodedArray);
            var bytes = Convert.FromBase64String(bytesCoded);
            return new ImageInfo(bytes, width, height);
        }
        
        private void RenderImage(ImageInfo imageInfo)
        {
            var texture = new Texture2D(imageInfo.Width, imageInfo.Height);
            texture.LoadImage(imageInfo.Bytes);
            ImageResizer.AdjustImageToTexture(image, new Vector2(imageInfo.Width, imageInfo.Height), ImageResizer.AdjustMode.ToMinimum);
            UnityEngine.Object.Destroy(receivedTexture);
            receivedTexture = texture;
            var rects = tracker.CurrentRects;
            var mat = OpenCvSharp.Unity.TextureToMat(texture);
            foreach (var rect in rects)
            {
                Cv2.Rectangle(mat, rect, Scalar.Red, 3);
            }
            UnityEngine.Object.Destroy(image.texture);
            image.texture = OpenCvSharp.Unity.MatToTexture(mat);
        }

        public async Task SendSelection(CancellationToken cancellationToken)
        {
            var selectRects = tracker.CurrentRects;
            await WriteInt(selectionStream, selectRects.Count, cancellationToken);
            foreach (var selectRect in selectRects)
            {
                await WriteInt(selectionStream, selectRect.Location.X, cancellationToken);
                await WriteInt(selectionStream, selectRect.Location.Y, cancellationToken);
                await WriteInt(selectionStream, selectRect.Size.Width, cancellationToken);
                await WriteInt(selectionStream, selectRect.Size.Height, cancellationToken);
            }
        }
    }
}