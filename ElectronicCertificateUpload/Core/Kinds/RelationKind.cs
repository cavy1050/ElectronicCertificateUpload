using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Core
{
    public class RelationKind
    {
        /// <summary>
        /// 票据端就诊信息主键 ULID编码
        /// </summary>
        public string BILL_MDTRT_INFO_ID { get; set; }

        /// <summary>
        /// 票据端结算信息主键 ULID编码
        /// </summary>
        public string SETL_BILL_ID { get; set; }

        /// <summary>
        /// 医院端门诊挂号序号
        /// </summary>
        public long HIS_GHXH { get; set; }

        /// <summary>
        /// 医院端门诊结算收据号
        /// </summary>
        public string HIS_JSSJH { get; set; }

        /// <summary>
        /// 医院端住院首页序号
        /// </summary>
        public long HIS_SYXH { get; set; }

        /// <summary>
        /// 医院端住院结算序号
        /// </summary>
        public long HIS_JSXH { get; set; }

        /// <summary>
        /// 结算日期 (YYYY-MM-DD hh:mm:ss)
        /// </summary>
        public string BILL_TIME { get; set; }

        /// <summary>
        /// 自费标志 0:非自费病人 1:自费病人
        /// </summary>
        public string SELF_FUNDED_FLAG { get; set; }

        /// <summary>
        /// 红冲标志:0未红冲;1已红冲
        /// </summary>
        public string RHRED_FLAG { get; set; }

        /// <summary>
        /// 上传状态:1已生成主记录;2已生成明细记录;3已上传主记录;4已上传明细记录
        /// </summary>
        public string UPLOAD_STAS { get; set; }
    }
}
