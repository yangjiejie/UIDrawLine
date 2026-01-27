
using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;



namespace SCG
{
    public class UIDrawLine : MonoBehaviour
    {
        public int cellX = 3;
        public int cellY = 3;
        public float spaceX = 250;
        public float spaceY = 250;
        GameObject linkLineNode;
        List<UVertex> allUIVertexs;
        List<UILine> allLines;
        RectTransform rect;
        RectTransform Rect
        {
            get
            {
                rect = rect ?? transform as RectTransform;
                return rect;
            }

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
            //for (int i = 0; i < allUIVertexs.Count - 1; i++)
            //{
            //    LinkLine(allUIVertexs[i], allUIVertexs[i + 1]);
            //}
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
    }
}


