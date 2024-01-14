using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Services
{
    /// <summary>
    /// 生成接口
    /// </summary>
    public interface IGenerateController : IController
    {
        /// <summary>
        /// 比对源数据库与本地库记录
        /// </summary>
        Task<int> CompareNativeFromSourceRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg, bool selfFundedFlagArg);

        /// <summary>
        /// 根据源数据生成本地记录
        /// </summary>
        Task<bool> SynchronizeNativeFromSourceRemoteRecord();
    }
}
