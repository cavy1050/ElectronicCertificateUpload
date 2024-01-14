using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Services
{
    /// <summary>
    /// 生成、上传公共接口
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// 当前记录数
        /// </summary>
        int CurrentCount { get; set; }

        /// <summary>
        /// 记录处理事件,更新当前记录数属性
        /// </summary>
        event EventHandler Ticked;
    }
}
