using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace SCG 
{ 
    public  class UIDrawPrevivew
    {
        private Stack<UVertex> record = new Stack<UVertex>();
        private Dictionary<UILine, GameObject> uvMap = new();
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

        public bool HasDrawLine(UILine line)
        {
            return uvMap.Count >0 && uvMap.ContainsKey(line);
        }
        public void DrawLine(UVertex startVertex,UILine line,float k,float lineWidth = 33)
        {
            if (line == null) return;
            GameObject go = null;
            var form = startVertex == line.form ? line.form : line.to;
            var to = form == line.form ? line.to : line.form;
            var endPos = form.GetPos() + k * (to.GetPos() - form.GetPos());
            if (!uvMap.ContainsKey(line))
            {
                var lineObj = PoolManager.Get("Assets/Art/Test/Line.prefab");
                uvMap.Add(line, lineObj);
                go = uvMap[line];                
                go.transform.SetParent(this.root.transform, false);
                if(form == line.form)
                {
                    go.Init(line.ToString(), "UI");
                }
                else
                {
                    go.Init($"line{form.id}_{to.id}", "UI");
                }

                go.GetComponent<Image>().color = Color.green;
            }
            else
            {
                go = uvMap[line];
            }            
            var rect = go.transform as RectTransform;
            var length = Vector2.Distance(form.GetPos(), endPos);
            var dir = (to.GetPos() - form.GetPos()).normalized;
            Debug.Log($"调整{go.name}的长度为{length}");
            rect.sizeDelta = new Vector2(length, lineWidth);
            rect.pivot = new Vector2(0, 0.5f);
           
            var sPos = RectTransformUtility.WorldToScreenPoint(Camera.current, form.go.transform.position);
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
