


using UnityEngine;


namespace SCG
{
    public class UILine 
    {
        public UVertex form;
        public UVertex to;
        public int id;
        public GameObject go;

        public bool isReacth = false;
        public bool IsShow { get; set; } = true;
        private static string linePrefabUrl = "Assets/Art/Test/Line.prefab";
        private static bool showIdOrName = false;
        public float lineWidth { get; set; } = 10;
        public static int UUID = 0;
        public static UILine Create(  UVertex _form,UVertex _to, Transform parent , string lineResUrl = null)
        {
            if(string.IsNullOrEmpty( lineResUrl))
            {
                lineResUrl = linePrefabUrl;
            }    
            var go = PoolManager.Get(linePrefabUrl);            
            go.transform.SetParent(parent, false);
            var line = new UILine();
            line.id = ++UUID;
            line.form = _form;
            line.to = _to;
            line.go = go;
            go.Init(line.ToString(), "UI");
            return line;    
        }

        public override string ToString()
        {
            if(showIdOrName)
                return $"line{form}-{to}";
            return $"line{form.go.name}-{to.go.name}";
        }

        public void Destroy()
        {
            if (this.go)
            {
                GameObject.Destroy(this.go);
            }
            this.form = null;
            this.to = null;
            this.id = 0;           
            this.go = null;
            
        }

        public void Draw()
        {
            go.SetActiveX(true);
            var rect = this.go.transform as RectTransform;
            var length = CommonUtils.GetBetweenUIVertexLength(form, to);
            var dir = CommonUtils.GetBetweenUIVertexDir(form, to);
            rect.sizeDelta = new Vector2(length, lineWidth);
            rect.pivot = new Vector2(0,0.5f);
            var parent = this.go.transform.parent;
            var sPos = RectTransformUtility.WorldToScreenPoint(Camera.current,form.go.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent as RectTransform, sPos,Camera.current,out Vector2 localPos);

            rect.anchoredPosition3D = localPos;
            rect.localRotation = Quaternion.FromToRotation(Vector3.right, dir);
        }

        public void ShowActive(bool show)
        {
            show = show && this.form.IsShow() && this.to.IsShow();
            IsShow = show;
            go?.SetActiveX(show);
        }

        
    }
}
