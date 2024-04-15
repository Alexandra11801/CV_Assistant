using OpenCvSharp.Tracking;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CVAssistant.ObjectsTracking
{
    public class ObjectSelection : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] RectTransform rectTransform;
        private Tracker tracker;
        private ObjectsTracker objectsTracker;

        public RectTransform RectTransform => rectTransform;
        
        public Tracker Tracker
        {
            get { return tracker; }
            set { tracker = value; }
        }

        public ObjectsTracker ObjectsTracker
        {
            get { return objectsTracker; }
            set { objectsTracker = value; }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.clickCount == 2)
            {
                tracker.Dispose();
                objectsTracker.RemoveSelection(this);
                Destroy(gameObject);
            }
        }
    }
}
