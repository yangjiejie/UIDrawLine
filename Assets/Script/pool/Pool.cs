using System;
using System.Collections.Generic;


//思考 考虑使用弱引用来做容器 如果强引用mono可能mono本身会被释放掉 
//这样容器中的对象可能已经失效 但是弱引用也会有性能上的开销 
//关于这一块 我在spine的动画接口封装上有实战 暂用List容器
namespace SCG
{
    public class Pool<T> 
    {
        public static List<T> objects = new();
        /// <summary>
        /// 使用一个对象池的时候直接get 
        /// </summary>
        public static T Get (Func<T> create) 
        {
           
            T result = default;
            
            if(objects.Count > 0)
            {
                while (objects.Count > 0)
                {

                    var last = objects[objects.Count - 1];
                    if (last == null)
                    {
                        objects.RemoveAt(objects.Count - 1);
                        continue;
                    }
                    else
                    {
                        result = last ;
                        objects.RemoveAt(objects.Count - 1);
                        break;
                    }
                }
            }
            else
            {
                result = create();                
            }
            if(result is IPool x)
            {
                x.OnGet();
            }
         
            return result; 
        }
        /// <summary>
        /// 说明一点即使不手动Release也是可以的 要支持这一点 
        /// </summary>
        public static void Release(T t,bool bRecycle = true)
        {
            if(t is IPool)
            {
                (t as IPool).OnRelease();
            }
            // else 例如纯mono的gameObject 
            if(bRecycle)
            {
                objects.Add(t);
            }                
        }
        /// <summary>
        /// 清理但是不放到对象池 
        /// </summary>
        internal static void ReleaseAll()
        {
            if(objects != null && objects.Count > 0)
            {
                for (int i = objects.Count - 1; i >= 0; --i)
                {
                    Release(objects[i],false);
                }
            }            
            objects?.Clear();
        }
    }
}
