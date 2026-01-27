using System;
using System.Collections.Generic;

using SCG;
using UnityEngine;

//desc 我需要一个对象池实现 仅仅只用manager调用Get即可 需要覆盖一下3类使用场景 

//第一类对象次 普通c#对象 cs           需要实现IPool接口 
//第二类对象池 比如mono对象gameObject  不需要实现IPool接口 
//第三类对象池 自定义的组件 component  需要实现IPool接口  
//上诉三类对象池比较常用 故此封装一他们的使用 
//特别的 对于第二类和第三类 也存在动态加载的情况，因此也提供了url接口 即Get会附带一个url加载链接

//另外需要提供对象次的统一清理接口，防止有人在某些场景未手动回收，我们需要在转场的时候清理 ReigsterRelease

namespace SCG
{
    public class PoolManager
    {
        private const int CSNormalObj = 1;
        private const int MonoGameObject = 2;
        private const int MonoComponent = 3;

        private static Dictionary<string, HashSet<Action>> poolMap = null;

        private static GameObject PoolRoot;
        public static void Init()
        {
            if(PoolRoot == null)
            {
                var go = new GameObject("PoolRoot");
                GameObject.DontDestroyOnLoad(go);
            }
        }
       
        /// <summary>
        /// 加载预设资源并且用于对象池 
        /// </summary>
        /// <param name="prefabRes"></param>
        /// <returns></returns>
        public static GameObject Get(string prefabRes) 
        {
            
            var itemPrefab =  LoadResApi.LoadRes<GameObject>(prefabRes);
            return Get(itemPrefab);
        }
        /// <summary>
        ///  获取一个已经存在的gameObject并且以此为基础的对象 常见于滚动视图中的item
        ///  可能需要n个item的情况
        /// </summary>
        /// <param name="prefabTemplte"></param>
        /// <returns></returns>
        public static GameObject Get(GameObject prefabTemplte)
        {
            var result = Pool<GameObject>.Get(() =>
            {
                var ins = GameObject.Instantiate(prefabTemplte);
                return ins;
            });
            ReigsterRelease<GameObject>(prefabTemplte.GetHierarchyPath());  
            return result;
        }

        private static void ReigsterRelease<T>(string key)
        {
            poolMap = poolMap ?? new();
            if (!poolMap.ContainsKey(key))
            {
                poolMap.Add(key, new HashSet<Action>());
                poolMap[key].Add(Pool<T>.ReleaseAll);
            }
            else if (!poolMap[key].Contains(Pool<T>.ReleaseAll))
            {
                poolMap[key].Add(Pool<T>.ReleaseAll);
            }
        }
        public static T Get<T>() where T : IPool, new() // 普通c#对象
        {
            var result = Pool<T>.Get(() => new T());
            ReigsterRelease<T>(typeof(T).FullName);
            return result;
        }
        /// <summary>
        /// 获取脚本对象的对象池 (复用)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="bindGo"></param>
        /// <returns></returns>
        public static T Get<T>(GameObject bindGo) where T : Component, IPool, new() // 普通脚本对象
        {   
            var result = Pool<T>.Get(() =>
            {
                var go = GameObject.Instantiate<GameObject>(bindGo);
                var com = go.GetComponent<T>();
                if (com == null)
                {
                    com = go.AddComponent<T>();
                }
                return com;
            });
            if(result == null)
            {
                PoolManager.Destroy<T>(bindGo.GetHierarchyPath());
                result = Pool<T>.Get(() =>
                {
                    var go = GameObject.Instantiate<GameObject>(bindGo);
                    var com = go.GetComponent<T>();
                    if (com == null)
                    {
                        com = go.AddComponent<T>();
                    }
                    return com;
                });
            }
            if (result == null)
            {
                Debug.LogError("对象池异常");
            }
            ReigsterRelease<T>(bindGo.GetHierarchyPath());
            return result;
        }

        public static T Get<T>( string prefabRes) where T : Component, IPool, new() // 普通脚本对象
        {
            var result =  Pool<T>.Get( () =>
            {

                var go =  LoadResApi.LoadRes<GameObject>(prefabRes);
                var goIns = GameObject.Instantiate(go);
                var com = goIns.GetComponent<T>();
                if (com == null)
                {
                    com = goIns.AddComponent<T>();
                }
                return com;
            });
            //脚本对象无效了 
            if(result == null )
            {
                PoolManager.Destroy<T>(prefabRes);
                result = Pool<T>.Get(() =>
                {

                    var go = LoadResApi.LoadRes<GameObject>(prefabRes);
                    var goIns = GameObject.Instantiate(go);
                    var com = goIns.GetComponent<T>();
                    if (com == null)
                    {
                        com = goIns.AddComponent<T>();
                    }
                    return com;
                });
            }
            if(result == null)
            {
                Debug.LogError("对象池异常");
            }
            ReigsterRelease<T>(prefabRes);  
            return result;
        }
        public static void Destroy<T>(string key = "")
        {
            if (poolMap == null || poolMap.Count == 0) return;
            if(string.IsNullOrEmpty(key))
            {
                key = typeof(T).FullName;
            }
            if(!poolMap.ContainsKey(key))
            {
                return;
            }
            var hashSet = poolMap[key];
            foreach(var va in hashSet)
            {
                va.Invoke();
            }
            hashSet.Clear();
            poolMap.Remove(key);
        }
        
        public static void Release<T>(T t)
        {
            if (t == null) return;
            Pool<T>.Release(t); 
        }

    }
}
