using OpenCvSharp;
using OpenCvSharp.Tracking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rect = OpenCvSharp.Rect;

namespace CVAssistant.ObjectsTracking
{
    public class ObjectsTracker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RawImage image;
        [SerializeField] private double minimalSelectDiagonal;
        [SerializeField] private int trackersLimit;
        [SerializeField] private GameObject objectSelectionPrefab;
        private List<ObjectSelection> currentSelections;
        private Vector2 dragStartPosition;
        private Vector2 dragEndPosition;
        private bool isDragging;

        private void Awake()
        {
            dragStartPosition = Vector2.zero;
            dragEndPosition = Vector2.zero;
            currentSelections = new List<ObjectSelection>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragStartPosition = eventData.position;
            dragEndPosition = dragStartPosition;
            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            dragEndPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragEndPosition = eventData.position;
            isDragging = false;
        }

        public List<Rect> RenderSelectRects()
        {
            var selectRects = new List<Rect>();
            var mat = OpenCvSharp.Unity.TextureToMat(image.texture as Texture2D);
            var imageSize = new Size(image.rectTransform.sizeDelta.x, image.rectTransform.sizeDelta.y);
            var delimeter = (float)mat.Size().Width / imageSize.Width;
            if(dragStartPosition != Vector2.zero || dragEndPosition != Vector2.zero)
            {
                var start = ToImageSpace(dragStartPosition, imageSize, delimeter);
                var end = ToImageSpace(dragEndPosition, imageSize, delimeter);
                var selectPoint = new Point(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y));
                var selectSize = new Size(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
                var selectRect = new Rect(selectPoint, selectSize);
                if (!isDragging)
                {
                    if ((end - start).magnitude >= minimalSelectDiagonal && currentSelections.Count < trackersLimit)
                    {
                        var obj = new Rect2d(selectRect.X, selectRect.Y, selectRect.Width, selectRect.Height);
                        var tracker = Tracker.Create(TrackerTypes.KCF);
                        tracker.Init(mat, obj);
                        var newSelection = Instantiate(objectSelectionPrefab, Vector2.zero, Quaternion.identity, image.transform).GetComponent<ObjectSelection>();
                        newSelection.RectTransform.sizeDelta = new Vector2(selectRect.Width, selectRect.Height);
                        newSelection.Tracker = tracker;
                        newSelection.ObjectsTracker = this;
                        currentSelections.Add(newSelection);
                        dragStartPosition = Vector2.zero;
                        dragEndPosition = Vector2.zero;
                    }
                }
                else
                {
                    selectRects.Add(selectRect);
                }
            }
            if(currentSelections.Count > 0)
            {
                var count = currentSelections.Count;
                var i = 0;
                while(i < count)
                {
                    var obj = Rect2d.Empty;
                    var selection = currentSelections[i];
                    var tracker = selection.Tracker;
                    var trackerUpdated = tracker.Update(mat, ref obj);
                    if (!trackerUpdated)
                    {
                        obj = Rect2d.Empty;
                        tracker.Dispose();
                        currentSelections.Remove(selection);
                        count--;
                    }
                    else
                    {
                        selection.transform.localPosition = new Vector2((float)obj.X / delimeter - imageSize.Width / 2 + (float)obj.Width / (2 * delimeter), 
                            imageSize.Height / 2 - (float)obj.Y / delimeter - (float)obj.Height / (2 * delimeter));
                        selection.RectTransform.sizeDelta = new Vector2((float)obj.Width, (float)obj.Height) / delimeter;
                        selectRects.Add(new Rect((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
                        i++;
                    }
                }
            }
            if (selectRects.Count > 0)
            {
                foreach (var rect in selectRects)
                {
                    Cv2.Rectangle(mat, rect, Scalar.Red, 3);
                }
                image.texture = OpenCvSharp.Unity.MatToTexture(mat);
            }
            return selectRects;
        }

        private Vector2 ToImageSpace(Vector2 point, Size size, float delimeter)
        {
            Vector2 result = new Vector2();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, point, Camera.main, out result);
            result.x += size.Width * 0.5f;
            result.y += size.Height * 0.5f;
            result.y = size.Height - result.y;
            result.x = Mathf.Clamp(result.x, 0, size.Width);
            result.y = Mathf.Clamp(result.y, 0, size.Height);
            result *= delimeter;
            return result;
        }

        public void RemoveSelection(ObjectSelection selection)
        {
            currentSelections.Remove(selection);
        }
    }
}
