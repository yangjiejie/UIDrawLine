using System;
using System.Resources;




#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace SCG
{
    public  class LoadResApi
    {
        public static T LoadRes<T>(string resUrl) where T : UnityEngine.Object
        {
            T result = null;
            //if(!resUrl.StartsWith("Assets"))  // 尽量减少判断 因为是对象池 调用比较频繁能省性能则省 
            //{
            //    UnityEngine.Debug.LogError("资源必须是Assets开头");
            //}
           
#if UNITY_EDITOR
            try
            {
                result = AssetDatabase.LoadAssetAtPath<T>(resUrl);
                return result;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
#else
             result =  AssetLoader.Instance.LoadAssetSync<T>(resUrl);
#endif

            return result;
        }
    }
}
