using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Services
{
    /// <summary>
    /// 上传接口
    /// </summary>
    public interface IUpLoadController : IController
    {
        /// <summary>
        /// 比对目标数据库与本地库记录
        /// </summary>
        Task<int> CompareNativeToTargetRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg);

        /// <summary>
        /// 根据本地记录生成目标记录
        /// </summary>
        Task<bool> SynchronizeNativeToTargetRemoteRecord();
    }
}
