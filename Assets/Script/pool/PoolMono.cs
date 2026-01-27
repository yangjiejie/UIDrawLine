using System.Collections;

using UnityEngine;
//PoolManager已经实现了get和release接口 基本上能做对象池的需求了
// 但是做unity开发有时候需要手动挂脚本，指定一些额外的参数 ，比如对象池的url，
// 或者生成的模版item 通过拖拽赋值 设置父亲节点 出生点 因此也附加这个脚本
namespace SCG
{
    public class PoolMono : MonoBehaviour
    {
        [LabelText("出生点父亲节点")]
        public Transform bindFather;
        public Vector3 scale = Vector3.one;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 localPos = Vector3.zero;
        [LabelText("生成模版")]
        public GameObject itemPrefab;
        [LabelText("动态资源url")]
        public string resUrl;
        [ShowInInspector("创建对象池对象")]
        public void Spawn()
        {
            if(string.IsNullOrEmpty(resUrl))
            {
                if(itemPrefab == null)
                {
                    Debug.LogError("请指定url或者模版");
                    return;
                }
                else
                {
                    var go = PoolManager.Get(itemPrefab);
                    initGo(go);
                }
            }
            else
            {
                var go = PoolManager.Get(resUrl);
                initGo(go);
            }

        }
        void initGo(GameObject go)
        {
            go.name = "出生";
            go.transform.SetParent(bindFather, false);
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            go.transform.localPosition = localPos;
        }
    }
}