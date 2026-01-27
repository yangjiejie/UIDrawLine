using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace SCG
{
    public   class UVertex
    {
        public GameObject go;
        public int id;
        public bool isShow = true;
        public override string ToString()
        {
            return $"{id}";
        }
        public void Destroy()
        {
            GameObject.Destroy(go);
        }

        public bool IsShow()
        {
            return isShow;
        }
        public void ShowActive(bool show)
        {
            isShow = show;
            go?.SetActiveX(show);
        }
    }
}
