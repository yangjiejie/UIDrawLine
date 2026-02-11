using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace SCG 
{ 
    public  class UIDrawPrevivew
    {
        private Stack<UVertex> record = new Stack<UVertex>();
        private Dictionary<UVertex, GameObject> uvMap = new();
        private GameObject root = null;
        public UIDrawPrevivew(GameObject root)
        {
            this.root = root; 
        }
        public void AddRecord(UVertex tmpVertex)
        {
            record = record ?? new();
            if(record.Count == 0)
            {
                record.Push(tmpVertex);
            }
            else
            {
                var top = record.Peek();
                if(top == tmpVertex)
                {
                    return;
                }
                record.Push(tmpVertex);
            }
            
        }
        public void DrawLine(UVertex vertex,Vector2 endPos,float lineWidth = 33)
        {
            if (vertex == null) return;
            GameObject go = null;
            if(!uvMap.ContainsKey(vertex))
            {
                var lineObj = PoolManager.Get("Assets/Art/Test/Line.prefab");
                uvMap.Add(vertex, lineObj);
                go = uvMap[vertex];                
                go.transform.SetParent(this.root.transform, false);
                go.Init("tmp","UI");
                go.GetComponent<Image>().color = Color.green;
            }
            else
            {
                go = uvMap[vertex];
            }
               
            
            var rect = go.transform as RectTransform;
            var length = Vector2.Distance((vertex.go.transform as RectTransform).anchoredPosition, endPos);
            var dir = endPos - (vertex.go.transform as RectTransform).anchoredPosition;
            rect.sizeDelta = new Vector2(length, lineWidth);
            rect.pivot = new Vector2(0, 0.5f);
        
            var sPos = RectTransformUtility.WorldToScreenPoint(Camera.current, vertex.go.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root.transform as RectTransform, sPos, Camera.current, out Vector2 localPos);

            rect.anchoredPosition3D = localPos;
            rect.localRotation = Quaternion.FromToRotation(Vector3.right, dir);
        }

        public void CleanAll()
        {
            foreach(var item in uvMap)
            {
                if(item.Value)
                {
                    GameObject.Destroy(item.Value);
                }
            }
            uvMap.Clear();
        }
        public bool HasDrawer()
        {
            return this.uvMap.Count > 0;
        }
    }
}
