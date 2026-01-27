using System;

using UnityEngine;


namespace SCG
{
    public static class CommonUtils
    {
        public static T TryAddOrGetComponent<T>(this GameObject go) where T : Component
        {
            T com = go.GetComponent<T>();
            if (com == null)
            {
                com = go.AddComponent<T>();
            }
            return com;
        }
        /// <summary>
		/// 获得GameObject在Hierarchy中的完整路径
		/// </summary>
		public static string GetHierarchyPath(this GameObject go)
        {
            if (go == null) return string.Empty;
            if (!go.transform.parent)
            {
                return go.transform.name;
            }
            return GetHierarchyPath(go.transform.parent.gameObject) + "/" + go.transform.name;
        }
        public static void SetActiveX(this Component go, bool show)
        {
            SetActiveX(go.gameObject, show);
        }
        public static void SetActiveX(this GameObject go,bool show)
        {
            if(go.activeInHierarchy != show)
            {
                go.SetActive(show) ;
            }
        }
        public static GameObject CreateGameObject(string name,Transform parent, params Type[] ty )
        {
            if(string.IsNullOrEmpty(name))
            {
                name = "";
            }
            var go = new GameObject(name,ty);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        public static GameObject Init(this GameObject go, string name, string layer){
            if(!string.IsNullOrEmpty(layer))
            {
                go.layer = LayerMask.NameToLayer(layer);
            }            
            if(!string.IsNullOrEmpty(name))
            {
                go.name = name;
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        public static float GetBetweenUIVertexLength(UVertex form,UVertex to)
        {
            var rectForm = form.go.transform as RectTransform;
            var rectTo = to.go.transform as RectTransform;
            var vec = rectTo.anchoredPosition - rectForm.anchoredPosition;
            return vec.magnitude;
        }

        public static Vector2 GetBetweenUIVertexDir(UVertex form, UVertex to)
        {
            var rectForm = form.go.transform as RectTransform;
            var rectTo = to.go.transform as RectTransform;
            var vec = rectTo.anchoredPosition - rectForm.anchoredPosition;
            return vec.normalized;
        }

    }
}
