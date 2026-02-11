
using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace SCG
{
    public class UIDrawLine : MonoBehaviour,IDragHandler,IBeginDragHandler,IEndDragHandler, IDropHandler,IPointerClickHandler
        ,IPointerDownHandler
    {
        public int cellX = 3;
        public int cellY = 3;
        public float spaceX = 250;
        public float spaceY = 250;
        public UVertex currentSelectObj;
        public UVertex lastSelectObj;
        public UVertex startVertex;
        public Button resetButton;
        GameObject linkLineNode;
        List<UVertex> allUIVertexs;
        List<UILine> allLines;
        RectTransform rect;
        UIDrawPrevivew previewDrawLine;
        public Text curSelectTextInfo;
        RectTransform Rect
        {
            get
            {
                rect = rect ?? transform as RectTransform;
                return rect;
            }

        }
        void Awake()
        {
            var preview = new GameObject("",typeof(RectTransform));
            preview.name = "drawLinePreview";
            preview.transform.SetParent(this.transform, false);
            previewDrawLine = new UIDrawPrevivew(preview);
            DrawMap();
            resetButton?.onClick.AddListener(OnClickReset);
        }
        void OnClickReset()
        {
            previewDrawLine?.CleanAll();
            
            DrawHighLight(startVertex, false);
            DrawHighLight(currentSelectObj, false);
            DrawHighLight(lastSelectObj, false);
            this.currentSelectObj = null;
            this.lastSelectObj = null;
            
        }
        void CreateLinkLineNode()
        {
            if (linkLineNode == null)
            {
                linkLineNode = linkLineNode ?? CommonUtils.CreateGameObject(nameof(linkLineNode), transform, typeof(RectTransform));
                linkLineNode.transform.SetAsFirstSibling();
            }
        }
        [ShowInInspector("绘制线条-测试用例")]
        void DrawMap()
        {
            Clear();
            Rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.pivot = new Vector2(0.5f, 0.5f);
            allUIVertexs = allUIVertexs ?? new List<UVertex>();


            for (int y = 0; y < cellX; y++)
            {
                for (int x = 0; x < cellY; x++)
                {
                    var go = CommonUtils.CreateGameObject("", transform, typeof(RectTransform), typeof(Image));
                    go.layer = LayerMask.NameToLayer("UI");
                    (go.transform as RectTransform).anchoredPosition = new Vector2((x - 1) * spaceX, (y - 1) * spaceY);

                    var vartex = new UVertex();
                    vartex.go = go;
                    vartex.id = x + y * cellX;
                    
                    go.name = vartex.ToString();
                    allUIVertexs.Add(vartex);
                }
            }
            //
            //6,7,8
            //3,4,5
            //0,1,2
            //
            List<int> drawList = new List<int>()
            {
                0,1,1,2,2,5,5,8,8,7,7,6,6,3,3,0,3,4,4,5,7,4,4,1
            };
            for (int i = 0; i < drawList.Count - 1;)
            {
                LinkLine(drawList[i], drawList[i + 1]);
                i += 2;
            }
            
        }
        public GameObject testForm;
        public GameObject testTo;
        [ShowInInspector("测试添加线条")]
        public void Test()
        {
            LinkLine(testForm, testTo);
        }
       
        public void LinkLine(GameObject fromObj,GameObject toObj,float lineWidth = 33)
        {
            allUIVertexs = allUIVertexs ?? new();
            UVertex form = null, to = null;
            for(int i = 0; i < allUIVertexs.Count; i++)
            {
                if(allUIVertexs[i].go == fromObj)
                {
                    form = allUIVertexs[i];
                }
                if (allUIVertexs[i].go == toObj)
                {
                    to = allUIVertexs[i];
                }
                if(form != null && to != null)
                {
                    break;
                }
            }
           
            if(form == null)
            {
                var vartex = new UVertex();
                vartex.go = fromObj;
                vartex.id = fromObj.GetHashCode();

             
                allUIVertexs.Add(vartex);
                form = vartex;
            }
            if(to == null)
            {
                var vartex = new UVertex();
                vartex.go = toObj;
                vartex.id = toObj.GetHashCode();

            
                allUIVertexs.Add(vartex);
                to = vartex;
            }
            LinkLine(form, to,lineWidth);
        }
        void LinkLine(int aa, int bb,float lineWidth = 33)
        {
            var form = allUIVertexs.Find((xx) => xx.id == aa);
            var to = allUIVertexs.Find((xx) => xx.id == bb);
            LinkLine(form, to,lineWidth);
        }
        void LinkLine(UVertex from, UVertex to,float lineWidth = 33)
        {
            if(from == null || to == null)
            {
                Debug.LogError("form or to == null");
                return;
            }
            allLines = allLines ?? new();
            // 优化linq内存 
            var result =  allLines.Find((xx) => xx.form == from && xx.to == to);
            if(result != null)
            {
                Debug.Log($"已经有重复的线条绘制{result}");
                return; // 不要重复去绘制 
            }
            CreateLinkLineNode();
            var uiLine = UILine.Create( from, to, linkLineNode.transform);
            uiLine.lineWidth = lineWidth;
            allLines.Add(uiLine);
            uiLine.Draw();
        }
        GameObject GetObj(int id )
        {
            var result = allUIVertexs.Find((xx) => xx.id == id);
            return result?.go;
        }
        [ShowInInspector("清空")]
        void Clear()
        {
            ClearAllLine();
            ClearAllCell();
            if (linkLineNode)
            {
                GameObject.Destroy(linkLineNode.gameObject);
            }
            linkLineNode = null;
        }
        [ShowInInspector("隐藏/显示所有")]
        void HideAll(bool isHide)
    {
            if(allLines != null)
            {
                for(int  i = allLines.Count- 1; i >= 0; --i)
                {
                    allLines[i]?.ShowActive(isHide);
                }
            }
            if (allUIVertexs != null)
            {
                for (int i = allUIVertexs.Count - 1; i >= 0; --i)
                {
                    allUIVertexs[i]?.ShowActive(isHide);
                }
            }
        }
        void ClearAllLine()
        {
            if (allLines != null)
            {
                foreach (var item in allLines)
                {
                    item.Destroy();
                }
                allLines?.Clear();
                allLines = null;
            }
        }
        void ClearAllCell()
        {
            if (allUIVertexs != null)
            {
                foreach (var go in allUIVertexs)
                {
                    go.Destroy();
                }
                allUIVertexs?.Clear();
                allUIVertexs = null;
            }
        }

        [ShowInInspector("界面销毁")]
        private void OnDestroy()
        {
            Clear();
        }
        [ShowInInspector("显示/隐藏cell")]
        public void ShowCell(int cellId,bool show)
        {
            var result = allUIVertexs.Find((x) => x.id == cellId);
            if (result == null) return;
            result?.ShowActive(show);
            if (allLines == null || allLines.Count == 0) return;
            var allFindResult = allLines.FindAll((x) => x.form == result || x.to == result);
            foreach(var item in allFindResult)
            {
                item?.ShowActive(show); 
            }

        }
        Vector2 GetRectUIPos(GameObject go)
        {
            return (go.transform as RectTransform).anchoredPosition;
        }
        Vector2 ToLocalPos(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rect, eventData.position, Camera.main, out Vector2 localPos);
            return localPos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (this.currentSelectObj == null) return;
            var localPos = ToLocalPos(eventData);//当前光标滑动到的位置 
            //记录的位置，当游戏走过一个点则记录一下
            var startPos = (this.currentSelectObj.go.transform as RectTransform).anchoredPosition;
            
            UILine uline = null; 
            foreach(var line in allLines)
            {
                if(IsInLine(line, startPos,localPos,10))
                {
                    uline = uline ?? line;                    
                    break;
                }                
            }
            if (uline == null) return;
            this.previewDrawLine.DrawLine(currentSelectObj, localPos);
            CheckVertex(uline,localPos);
            CheckOk();

        }
        //简单当前距离自己最近的顶点是哪个 然后它是否被绘制过 
        void CheckVertex(UILine line, Vector2 curPos)
        {
            var from = this.currentSelectObj == line.form ? line.form : line.to;
            var to = from == line.form ? line.to : line.form;
            
            var ab = GetRectUIPos(to.go) - GetRectUIPos(from.go);
            var dir = curPos - GetRectUIPos(currentSelectObj.go);
            float k = Vector2.Dot(dir, ab) / ab.sqrMagnitude;
            if (k >= 0.98f)
            {

                this.lastSelectObj = this.currentSelectObj;
                this.currentSelectObj = to;
            }
            this.curSelectTextInfo.text = currentSelectObj.go.name;
            Debug.Log($"比例{k}");

        }
        /// <summary>
        /// 检查是否完成了一笔画 
        /// </summary>
        void CheckOk()
        {

        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnEndDrag(PointerEventData eventData)
        {
           
        }

        public void OnDrop(PointerEventData eventData)
        {
            
        }

        
        public bool IsInLine(UILine line,Vector2 startPos, Vector2 p, float tolerance = 5f)
        {
            var a = GetRectUIPos(line.form.go);
            var b = GetRectUIPos(line.to.go);
            return IsInLine(a, b, p, tolerance);
        }

        public bool IsInLine(Vector2 a, Vector2 b, Vector2 p, float threshold = 5f, float epsilon = 1e-5f)
        {
            // 线段向量
            Vector2 ab = b - a;
            float abSqrLen = ab.sqrMagnitude;

            // 退化情况：A 和 B 几乎重合
            if (abSqrLen < epsilon)
            {
                return Vector2.Distance(p, a) <= threshold;
            }

            // 计算投影参数 t（0~1 在线段内）
            float t = Vector2.Dot(p - a, ab) / abSqrLen;

            // 不在线段范围内
            if (t < -epsilon || t > 1f + epsilon)
                return false;

            // 投影点
            Vector2 projection = a + t * ab;

            // 点到线段的距离
            float distance = Vector2.Distance(p, projection);

            return distance <= threshold;
        }
        bool IsPosNearly(Vector2 posA,Vector2 posB,float thresholdHeight = 0,float thresholdWidth = 0)
        {
            var rect = (allUIVertexs[0].go.transform as RectTransform);
            if(thresholdHeight == 0)
            {
                thresholdHeight = rect.rect.height / 2f;
            }
            if (thresholdWidth == 0)
            {
                thresholdWidth = rect.rect.width / 2f;
            }
            
            if (Mathf.Abs(posA.x - posB.x) <= thresholdWidth && Mathf.Abs(posA.y - posB.y) <= thresholdHeight)
            {
                return true;
            }
            return false;
        }
        //根据点击的点子选择最近的点 
        public void OnPointerClick(PointerEventData eventData)
        {
            if(previewDrawLine != null && this.previewDrawLine.HasDrawer())
            {
                return;
            }
            var localPos = ToLocalPos(eventData);            
            bool hasClickObj = false;
            lastSelectObj = this.currentSelectObj;
            foreach (var item in allUIVertexs)
            {
                if(IsPosNearly((item.go.transform as RectTransform).localPosition, localPos))
                {
                    hasClickObj = true;
                    if(this.currentSelectObj != item)
                    {
                        this.currentSelectObj = item;
                    }                                        
                    break;
                }
            } 
            if(hasClickObj)
            {
                startVertex = this.currentSelectObj;
                if (this.currentSelectObj != this.lastSelectObj)
                {
                    OnSelectObj(currentSelectObj, lastSelectObj);
                }
            }
            
        }
        void DrawHighLight(UVertex vertext,bool bShow = true)
        {
            if (vertext == null) return;
            var go = vertext.go;
            go.GetComponent<Image>().color = bShow ? Color.green : Color.white;
        }
        void OnSelectObj(UVertex curSelect,UVertex lastSelect)
        {
            if(lastSelect == null)
            {
                DrawHighLight(curSelect);
            }
            else
            {
                DrawHighLight(curSelect,true);
                DrawHighLight(lastSelect,false);
            }
            Debug.Log("当前选中的点为" + curSelect.go.name + ",id = " + curSelect.id);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            
        }
    }
}


