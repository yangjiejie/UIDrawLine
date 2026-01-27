using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//定义对象池接口 
namespace SCG
{
    public interface IPool
    {
        /// <summary>
        /// 调用Get的时候 执行 
        /// </summary>
        public void OnGet();
        /// <summary>
        /// 
        /// 
        /// </summary>
        public void OnRelease();

    }
}
