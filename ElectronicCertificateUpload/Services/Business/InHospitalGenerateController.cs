using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using NUlid;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    public class InHospitalGenerateController : IGenerateController
    {
        string query;

        IAsyncDbController nativedDbController, zyDbController;

        dynamic sourceRemoteMdtrtRec, soureRemoteBillMasterRec;
        IEnumerable<dynamic> sourceRemoteDiagHub, sourceRemoteOprnHub, sourceRemoteFeeDetailHub;

        public IList<RelationKind> DifferenceHub { get; set; }

        public int CurrentCount { get; set; }

        public event EventHandler Ticked;

        public InHospitalGenerateController(IContainerProvider containerProviderArg)
        {
            nativedDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.NativeDB.ToString());
            zyDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.ZYDB.ToString());

            DifferenceHub = new List<RelationKind>();
        }

        public async Task<int> CompareNativeFromSourceRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg, bool selfFundedFlagArg)
        {
            DateTime endAfterOneDayTime = endTimeArg.AddDays(1);

            query = "SELECT a.syxh 'HIS_SYXH',b.xh 'HIS_JSXH',dbo.fun_ConvertDateString(b.jsrq, 'DT') 'BILL_TIME','0' 'SELF_FUNDED_FLAG','0' 'RHRED_FLAG' " +
                    "FROM ZY_BRSYK a(nolock) " +
                    "INNER JOIN ZY_BRJSK b(nolock) ON a.syxh = b.syxh " +
                    "WHERE b.jsrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.jsrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                    "AND b.ybjszt = 2 " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_ZYJZJLK x (nolock) WHERE a.syxh = x.syxh AND x.ybdjzt != 2) " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_ZYJSXXJLK y (nolock) WHERE b.xh = y.jsxh) " +
                    "AND EXISTS (SELECT 1 FROM YY_DZFP_ZYFP z (nolock) WHERE b.xh = z.jsxh) " +
                    "UNION ALL " +
                    "SELECT a.syxh 'HIS_SYXH',b.xh 'HIS_JSXH',dbo.fun_ConvertDateString(b.jsrq, 'DT') 'BILL_TIME','0' 'SELF_FUNDED_FLAG','1' 'RHRED_FLAG' " +
                    "FROM ZY_BRSYK a(nolock) " +
                    "INNER JOIN ZY_BRJSK b(nolock) ON a.syxh = b.syxh " +
                    "WHERE b.jsrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.jsrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                    "AND b.ybjszt = 2 " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_ZYJZJLK x (nolock) WHERE a.syxh = x.syxh AND x.ybdjzt != 2) " +
                    "AND EXISTS(SELECT 1 FROM YB_SI21_ZYJSXXJLK_QXJS c(nolock) WHERE b.hcxh = c.jsxh) " +
                    "AND EXISTS (SELECT 1 FROM YY_DZFP_ZYFP z (nolock) WHERE b.hcxh = z.jsxh)";

            IEnumerable <RelationKind> sourcePatientHub = await zyDbController.QueryAsync<RelationKind>(query);

            if (selfFundedFlagArg)
            {
                query = "SELECT a.syxh 'HIS_SYXH',b.xh 'HIS_JSXH',dbo.fun_ConvertDateString(b.jsrq, 'DT') 'BILL_TIME','1' 'SELF_FUNDED_FLAG',CASE WHEN b.jlzt = 1 THEN '1' ELSE '0' END 'RHRED_FLAG' "+
                        "FROM ZY_BRSYK a(nolock) "+
                        "INNER JOIN ZY_BRJSK b(nolock) ON a.syxh = b.syxh "+
                        "WHERE b.jsrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.jsrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                        "AND b.ybjszt = 2 " +
                        "AND b.ybdm = '101' "+
                        "AND EXISTS(SELECT 1 FROM YY_DZFP_ZYFP z(nolock) WHERE b.xh = z.jsxh)";

                IEnumerable<RelationKind> selfFundedPatientHub = await zyDbController.QueryAsync<RelationKind>(query);

                sourcePatientHub = selfFundedPatientHub == null ? sourcePatientHub : sourcePatientHub.Union(selfFundedPatientHub, new InHospitalGenerateRelationComparer());
            }

            query = "SELECT BILL_MDTRT_INFO_ID,SETL_BILL_ID,HIS_SYXH,HIS_JSXH,BILL_TIME,RHRED_FLAG,SELF_FUNDED_FLAG FROM RelationRecord " +
                    "WHERE BILL_TIME >= '" + beginTimeArg.ToString("yyyy-MM-dd") + "' AND BILL_TIME < '" + endAfterOneDayTime.ToString("yyyy-MM-dd") + "' " +
                    "AND MDTRT_TYPE = '2' " +
                    "AND UPLOAD_STAS IN ('2','3','4')";

            IEnumerable<RelationKind> targetPatientHub = await nativedDbController.QueryAsync<RelationKind>(query);

            query = "SELECT BILL_MDTRT_INFO_ID,SETL_BILL_ID,HIS_SYXH,HIS_JSXH,BILL_TIME,RHRED_FLAG,SELF_FUNDED_FLAG FROM RelationRecord " +
                    "WHERE BILL_TIME >= '" + beginTimeArg.ToString("yyyy-MM-dd") + "' AND BILL_TIME < '" + endAfterOneDayTime.ToString("yyyy-MM-dd") + "' " +
                    "AND MDTRT_TYPE = '2' " +
                    "AND UPLOAD_STAS = '1'";

            IEnumerable<RelationKind> generatedPatientHub = await nativedDbController.QueryAsync<RelationKind>(query);

            IEnumerable<RelationKind> comparedPatientHub = sourcePatientHub.Except(targetPatientHub, new InHospitalGenerateRelationComparer());

            DifferenceHub.Clear();

            foreach (RelationKind relation in comparedPatientHub)
            {
                if (generatedPatientHub.Contains(relation, new InHospitalGenerateRelationComparer()))
                    DifferenceHub.Add(new RelationKind
                    {
                        BILL_MDTRT_INFO_ID = generatedPatientHub.FirstOrDefault(x => x.HIS_JSXH == relation.HIS_JSXH).BILL_MDTRT_INFO_ID,
                        SETL_BILL_ID = generatedPatientHub.FirstOrDefault(x => x.HIS_JSXH == relation.HIS_JSXH).SETL_BILL_ID,
                        HIS_SYXH = relation.HIS_SYXH,
                        HIS_JSXH = relation.HIS_JSXH,
                        BILL_TIME = relation.BILL_TIME,
                        UPLOAD_STAS = "1",
                        RHRED_FLAG = relation.RHRED_FLAG,
                        SELF_FUNDED_FLAG = relation.SELF_FUNDED_FLAG
                    });
                else
                    DifferenceHub.Add(new RelationKind
                    {
                        BILL_MDTRT_INFO_ID = Ulid.NewUlid().ToString(),
                        SETL_BILL_ID = Ulid.NewUlid().ToString(),
                        HIS_SYXH = relation.HIS_SYXH,
                        HIS_JSXH = relation.HIS_JSXH,
                        BILL_TIME = relation.BILL_TIME,
                        UPLOAD_STAS = "0",
                        RHRED_FLAG = relation.RHRED_FLAG,
                        SELF_FUNDED_FLAG = relation.SELF_FUNDED_FLAG
                    });
            }

            return DifferenceHub.Count;
        }

        public async Task<bool> SynchronizeNativeFromSourceRemoteRecord()
        {
            bool ret = false;

            if (DifferenceHub.Count != 0)
            {
                for (CurrentCount = 1; CurrentCount <= DifferenceHub.Count; CurrentCount++)
                {
                    sourceRemoteMdtrtRec = soureRemoteBillMasterRec = sourceRemoteDiagHub = sourceRemoteOprnHub = sourceRemoteFeeDetailHub = null;

                    await FetchNativeFromSourceRemoteRecord(DifferenceHub.ElementAt(CurrentCount - 1));

                    await PushSourceRemoteIntoNaviteRecord(DifferenceHub.ElementAt(CurrentCount - 1));

                    Ticked?.Invoke(this, new EventArgs());
                }

                ret = true;
            }

            CurrentCount = CurrentCount <= DifferenceHub.Count ? CurrentCount : DifferenceHub.Count;

            return ret;
        }

        internal async Task FetchNativeFromSourceRemoteRecord(RelationKind relationArg)
        {
            await FetchMdtrtInfoFromSourceRemoteRecord(relationArg);
            await FetchBillInfoFromSourceRemoteRecord(relationArg);
            await FetchDiagInfoFromSourceRemoteRecord(relationArg);
            await FetchFeeDetailInfoFromSourceRemoteRecord(relationArg);
            await FetchOprnInfoFromSourceRemoteRecord(relationArg);
        }

        internal async Task PushSourceRemoteIntoNaviteRecord(RelationKind relationArg)
        {
            await PushRelationInfoIntoNaviteRecord(relationArg);
            await PushMdtrtInfoIntoNaviteRecord(relationArg);
            await PushBillInfoIntoNaviteRecord(relationArg);
            await PushDiagInfoIntoNaviteRecord(relationArg);
            await PushFeeDetailInfoIntoNaviteRecord(relationArg);
            await PushOprnInfoIntoNaviteRecord(relationArg);
            await FinishRelationInfoIntoNaviteRecord(relationArg);
        }

        internal async Task PushRelationInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (relationArg.UPLOAD_STAS != "1")
            {
                query = "INSERT INTO RelationRecord " +
                        "(BILL_MDTRT_INFO_ID,SETL_BILL_ID," +
                        "HIS_GHXH,HIS_JSSJH,HIS_SYXH,HIS_JSXH," +
                        "BILL_TIME,MDTRT_TYPE,RHRED_FLAG,SELF_FUNDED_FLAG," +
                        "UPLOAD_STAS,CRTE_TIME) " +
                        "VALUES('" + relationArg.BILL_MDTRT_INFO_ID + "', '" + relationArg.SETL_BILL_ID + "'," +
                        "0,0," + relationArg.HIS_SYXH + "," + relationArg.HIS_JSXH + ",'" +
                        relationArg.BILL_TIME + "', '2', '" + relationArg.RHRED_FLAG + "','" + relationArg.SELF_FUNDED_FLAG + "','" +
                        "1','" + DateTime.Now.ToString("G") + "') ";

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchMdtrtInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            if (relationArg.SELF_FUNDED_FLAG=="0")
            {
                query = "SELECT c.mdtrt_id 'BIZ_SN',a.sfzh 'CERT_NO',a.hzxm 'PSN_NAME',CASE WHEN a.sex='男' THEN '1' ELSE '2' END 'GEND'," +
                        "c.insutype 'INSUTYPE',c.psn_no 'HI_NO',c.insuplc_admdvs 'HI_ADMDVS_CODE',c.begntime 'BEGNTIME',CASE WHEN ISNULL(c.endtime,'')='' THEN dbo.fun_ConvertDateString(a.cqrq, 'DT') ELSE c.endtime END 'ENDTIME'," +
                        "c.med_type 'MED_TYPE',c.matn_type 'MATN_TYPE',c.birctrl_type 'BIRCTRL_TYPE',c.fetts 'FETTS'," +
                        "c.geso_val 'GESO_VAL',c.fetus_cnt 'FETUS_CNT',c.dscg_way 'DSCG_WAY',c.die_date 'DIE_DATE'," +
                        "c.ipt_no 'IPT_OTP_NO',c.medrcdno 'MEDRCDNO',c.atddr_no 'CHFPDR_CODE',c.chfpdr_name 'CHFPDR_NAME'," +
                        "c.adm_dept_codg 'ADM_CATY',c.dscg_dept_codg 'DSCG_CATY' " +
                        "FROM ZY_BRSYK a (nolock) " +
                        "INNER JOIN ZY_BRJSK b (nolock) ON a.syxh=b.syxh " +
                        "INNER JOIN YB_SI21_ZYJZJLK c (nolock) ON a.syxh=c.syxh AND c.ybdjzt != 2 " +
                        "WHERE a.syxh = " + relationArg.HIS_SYXH + " AND  b.xh=" + relationArg.HIS_JSXH;
            }
            else
            {
                query = "SELECT a.syxh 'BIZ_SN',a.sfzh 'CERT_NO',a.hzxm 'PSN_NAME',CASE WHEN a.sex='男' THEN '1' ELSE '2' END 'GEND', " +
                        "'' 'INSUTYPE','' 'HI_NO','' 'HI_ADMDVS_CODE',dbo.fun_ConvertDateString(a.rqrq, 'DT') 'BEGNTIME',dbo.fun_ConvertDateString(a.cqrq, 'DT') 'ENDTIME'," +
                        "'' 'MED_TYPE','' 'MATN_TYPE','' 'BIRCTRL_TYPE','' 'FETTS'," +
                        "'' 'GESO_VAL','' 'FETUS_CNT','' 'DSCG_WAY','' 'DIE_DATE'," +
                        "'' 'IPT_OTP_NO','' 'MEDRCDNO',a.ysdm 'CHFPDR_CODE',x.name 'CHFPDR_NAME'," +
                        "'' 'ADM_CATY','' 'DSCG_CATY' " +
                        "FROM ZY_BRSYK a (nolock) " +
                        "INNER JOIN ZY_BRJSK b (nolock) ON a.syxh=b.syxh " +
                        "INNER JOIN YY_ZGBMK x (NOLOCK) ON a.ysdm = x.id " +
                        "WHERE a.syxh = " + relationArg.HIS_SYXH + " AND  b.xh=" + relationArg.HIS_JSXH;
            }

            sourceRemoteMdtrtRec = await zyDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushMdtrtInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (sourceRemoteMdtrtRec != null)
            {
                if (relationArg.UPLOAD_STAS == "1")
                {
                    query = "DELETE FROM MdtrtRecord AS a WHERE a.BILL_MDTRT_INFO_ID ='" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await nativedDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO MdtrtRecord(BILL_MDTRT_INFO_ID, BIZ_SN," +
                        "CERT_NO, PSN_NAME, GEND, INSUTYPE, HI_NO, HI_ADMDVS_CODE," +
                        "BEGNTIME, ENDTIME,MED_TYPE, MATN_TYPE, BIRCTRL_TYPE," +
                        "FETTS, GESO_VAL, FETUS_CNT, DSCG_WAY, DIE_DATE," +
                        "IPT_OTP_NO, MEDRCDNO, CHFPDR_CODE, CHFPDR_NAME," +
                        "ADM_CATY,DSCG_CATY) " +
                        "VALUES('" + relationArg.BILL_MDTRT_INFO_ID + "','" + sourceRemoteMdtrtRec.BIZ_SN + "','" +
                        sourceRemoteMdtrtRec.CERT_NO + "','" + sourceRemoteMdtrtRec.PSN_NAME + "','" + sourceRemoteMdtrtRec.GEND + "','" + sourceRemoteMdtrtRec.INSUTYPE + "','" + sourceRemoteMdtrtRec.HI_NO + "','" + sourceRemoteMdtrtRec.HI_ADMDVS_CODE + "','" +
                        sourceRemoteMdtrtRec.BEGNTIME + "','" + sourceRemoteMdtrtRec.ENDTIME + "','" + sourceRemoteMdtrtRec.MED_TYPE + "','" + sourceRemoteMdtrtRec.MATN_TYPE + "','" + sourceRemoteMdtrtRec.BIRCTRL_TYPE + "','" +
                        sourceRemoteMdtrtRec.FETTS + "','" + sourceRemoteMdtrtRec.GESO_VAL + "','" + sourceRemoteMdtrtRec.FETUS_CNT + "','" + sourceRemoteMdtrtRec.DSCG_WAY + "','" + sourceRemoteMdtrtRec.DIE_DATE + "','" +
                        sourceRemoteMdtrtRec.IPT_OTP_NO + "','" + sourceRemoteMdtrtRec.MEDRCDNO + "','" + sourceRemoteMdtrtRec.CHFPDR_CODE + "','" + sourceRemoteMdtrtRec.CHFPDR_NAME + "','" +
                        sourceRemoteMdtrtRec.ADM_CATY + "','" + sourceRemoteMdtrtRec.DSCG_CATY + "')";
                

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchBillInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            if (relationArg.SELF_FUNDED_FLAG=="0")
            {
                query = "SELECT b.setl_id 'SETL_ID',a.zje 'MEDFEE_AMT'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.fulamt_ownpay_amt" : "-b.fulamt_ownpay_amt") + ",0) 'PSN_OWNPAY'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.hifp_pay" : "-b.hifp_pay") + ",0) 'HIFP_PAY_AMT'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.oth_pay" : "-b.oth_pay") + ",0) 'OTH_PAY'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.acct_pay" : "-b.acct_pay") + ",0) 'PSN_ACCT_PAY'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.psn_cash_pay" : "-b.psn_cash_pay") + ",0) 'PSN_CASH_PAY'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.psn_part_amt" : "-b.psn_part_amt") + ",0) 'PSN_SELFPAY'," +
                        "isnull(" + (relationArg.RHRED_FLAG == "0" ? "b.inscp_scp_amt" : "-b.inscp_scp_amt") + ",0) 'INSCP_AMT'," +
                        "c.pjdm 'BILL_CODE',c.pjhm 'BILL_NO',c.jym 'BILL_CHKCODE',c.kpd 'BILLER',c.kpd 'RECHKER'," +
                        "c.url 'FILE_URL'," +
                        "(SELECT x.pjdm FROM dbo.YY_DZFP_ZYFP x(nolock) where x.jsxh= a.hcxh) 'REL_ELEC_SETL_CERT_CODE'," +
                        "(SELECT y.pjhm FROM dbo.YY_DZFP_ZYFP y(nolock) where y.jsxh= a.hcxh) 'REL_ELEC_SETL_CERT_NO' " +
                        "FROM ZY_BRJSK a(nolock) " +
                        "INNER JOIN YB_SI21_ZYJSXXJLK b(nolock) ON " + (relationArg.RHRED_FLAG == "0" ? "a.xh" : "a.hcxh") + "= b.jsxh " +
                        "INNER JOIN YY_DZFP_ZYFP c(nolock) ON " + (relationArg.RHRED_FLAG == "0" ? "a.xh" : "a.hcxh") + "= c.jsxh " +
                        "WHERE a.xh = " + relationArg.HIS_JSXH;
            }
            else
            {
                query = "SELECT '' 'SETL_ID',a.zje 'MEDFEE_AMT'," +
                        "0 'PSN_OWNPAY',0 'HIFP_PAY_AMT',0 'OTH_PAY',0 'PSN_ACCT_PAY',0 'PSN_CASH_PAY',0 'PSN_SELFPAY',0 'INSCP_AMT'," +
                        "c.pjdm 'BILL_CODE',c.pjhm 'BILL_NO',c.jym 'BILL_CHKCODE',c.kpd 'BILLER',c.kpd 'RECHKER'," +
                        "c.url 'FILE_URL'," +
                        "(SELECT x.pjdm FROM dbo.YY_DZFP_ZYFP x(nolock) where x.jsxh= a.hcxh) 'REL_ELEC_SETL_CERT_CODE'," +
                        "(SELECT y.pjhm FROM dbo.YY_DZFP_ZYFP y(nolock) where y.jsxh= a.hcxh) 'REL_ELEC_SETL_CERT_NO' " +
                        "FROM ZY_BRJSK a(nolock) " +
                        "INNER JOIN YY_DZFP_ZYFP c(nolock) ON " + (relationArg.RHRED_FLAG == "0" ? "a.xh" : "a.hcxh") + "= c.jsxh " +
                        "WHERE a.xh = " + relationArg.HIS_JSXH;
            }

            soureRemoteBillMasterRec = await zyDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushBillInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (soureRemoteBillMasterRec != null)
            {
                if (relationArg.UPLOAD_STAS == "1")
                {
                    query = "DELETE FROM BillMasterRecord AS a WHERE a.SETL_BILL_ID='" + relationArg.SETL_BILL_ID + "'";

                    await nativedDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO BillMasterRecord " +
                        "(SETL_BILL_ID,BILL_STAS_ID,SETL_RLTS_ID,SETL_ID," +
                        "MEDFEE_AMT,PSN_OWNPAY,HIFP_PAY_AMT,OTH_PAY," +
                        "PSN_ACCT_PAY,PSN_CASH_PAY,PSN_SELFPAY,INSCP_AMT," +
                        "BILL_CODE,BILL_NO,BILL_CHKCODE," +
                        "BILLER,RECHKER,BILL_AMT,BILL_DATE,BILL_TIME," +
                        "FILE_URL,REL_ELEC_SETL_CERT_CODE,REL_ELEC_SETL_CERT_NO) " +
                        "VALUES('" + relationArg.SETL_BILL_ID + "','" + Ulid.NewUlid().ToString() + "','" + Ulid.NewUlid().ToString() + "','" + soureRemoteBillMasterRec.SETL_ID + "'," +
                        soureRemoteBillMasterRec.MEDFEE_AMT + "," + soureRemoteBillMasterRec.PSN_OWNPAY + "," + soureRemoteBillMasterRec.HIFP_PAY_AMT + "," + soureRemoteBillMasterRec.OTH_PAY + "," +
                        soureRemoteBillMasterRec.PSN_ACCT_PAY + "," + soureRemoteBillMasterRec.PSN_CASH_PAY + "," + soureRemoteBillMasterRec.PSN_SELFPAY + "," + soureRemoteBillMasterRec.INSCP_AMT + ",'" +
                        soureRemoteBillMasterRec.BILL_CODE + "','" + soureRemoteBillMasterRec.BILL_NO + "','" + soureRemoteBillMasterRec.BILL_CHKCODE + "','" +
                        soureRemoteBillMasterRec.BILLER + "','" + soureRemoteBillMasterRec.RECHKER + "'," + soureRemoteBillMasterRec.MEDFEE_AMT + ",'" + Convert.ToDateTime(relationArg.BILL_TIME.Substring(0, 10)).ToString("yyyyMMdd") + "','" + relationArg.BILL_TIME.Substring(11, 8) + "','" +
                        soureRemoteBillMasterRec.FILE_URL + "','" + soureRemoteBillMasterRec.REL_ELEC_SETL_CERT_CODE + "','" + soureRemoteBillMasterRec.REL_ELEC_SETL_CERT_NO + "')";

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchDiagInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            query = "SELECT a.zdlb 'DIAG_TYPE',CASE a.zdlx WHEN '0' then '1' ELSE '0' END 'MAINDIAG_FLAG'," +
                    "a.zddm 'DIAG_CODE',a.zdmc 'DIAG_NAME',dbo.fun_ConvertDateString(a.zdrq, 'DT') 'DIAG_TIME'," +
                    "a.zdysdm 'DIAG_DR_CODE',x.name 'DIAG_DR_NAME' " +
                    "FROM ZY_BRZDQK a(nolock) " +
                    "INNER JOIN YY_ZGBMK x(nolock) ON a.zdysdm = x.id " +
                    "WHERE a.zdlb = 2 AND a.syxh = " + relationArg.HIS_SYXH;

            sourceRemoteDiagHub = await zyDbController.QueryAsync(query);
        }

        internal async Task PushDiagInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (sourceRemoteDiagHub.Count() != 0)
            {
                if (relationArg.UPLOAD_STAS == "1")
                {
                    query = "DELETE FROM DiagRecord AS a WHERE a.BILL_MDTRT_INFO_ID='" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await nativedDbController.ExecuteAsync(query);
                }

                foreach (dynamic sourceRemoteDiagRec in sourceRemoteDiagHub)
                {
                    query = "INSERT INTO DiagRecord (DIAG_INFO_ID,BILL_MDTRT_INFO_ID," +
                            "DIAG_TYPE,MAINDIAG_FLAG," +
                            "DIAG_CODE,DIAG_NAME,DIAG_TIME," +
                            "DIAG_DR_CODE,DIAG_DR_NAME) " +
                            "VALUES('" + Ulid.NewUlid().ToString() + "','" + relationArg.BILL_MDTRT_INFO_ID + "','" +
                            sourceRemoteDiagRec.DIAG_TYPE + "','" + sourceRemoteDiagRec.MAINDIAG_FLAG + "','" +
                            sourceRemoteDiagRec.DIAG_CODE + "','" + sourceRemoteDiagRec.DIAG_NAME + "','" + sourceRemoteDiagRec.DIAG_TIME + "','" +
                            sourceRemoteDiagRec.DIAG_DR_CODE + "','" + sourceRemoteDiagRec.DIAG_DR_NAME + "')";

                    await nativedDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FetchFeeDetailInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            if (relationArg.SELF_FUNDED_FLAG=="0")
            {
                query = "SELECT dbo.fun_ConvertDateString(b.cfrq , 'DT') 'FEE_OCUR_TIME'," +
                        (relationArg.RHRED_FLAG == "0" ? "b.cnt" : "-b.cnt") + " 'CNT'," +
                        (relationArg.RHRED_FLAG == "0" ? "b.det_item_fee_sumamt" : "-b.det_item_fee_sumamt") + " 'DETITEM_FEE_SUMAMT'," +
                        "b.pric 'PRIC',b.med_chrgitm_type 'MED_CHRGITM_TYPE',b.dydm 'MEDLIST_CODG',b.xmmc 'MEDLIST_NAME'," +
                        "b.xmgg 'SPEC',b.chrgitm_lv 'CHRGITM_LV',b.ybshbz 'HOSP_APPR_FLAG' " +
                        "FROM ZY_BRJSK a (nolock) " +
                        "INNER JOIN VW_SI21_ZYFYMXK b (nolock) ON a.syxh=b.syxh " +
                        "WHERE a.syxh = " + relationArg.HIS_SYXH + " AND a.xh = " + relationArg.HIS_JSXH + " " +
                        "AND b.cfrq >= a.ksrq AND b.cfrq < a.jzrq ";
            }
            else
            {
                query = "SELECT dbo.fun_ConvertDateString(b.zxrq , 'DT') 'FEE_OCUR_TIME'," +
                        (relationArg.RHRED_FLAG == "0" ? "b.ypsl" : "-b.ypsl") + " 'CNT',"+
                        (relationArg.RHRED_FLAG == "0" ? "b.zje" : "-b.zje") + " 'DETITEM_FEE_SUMAMT'," +
                        "b.ypdj 'PRIC','' 'MED_CHRGITM_TYPE',b.ypdm 'MEDLIST_CODG',b.ypmc 'MEDLIST_NAME'," +
                        "b.ypgg 'SPEC','' 'CHRGITM_LV','' 'HOSP_APPR_FLAG' " +
                        "FROM ZY_BRJSK a (nolock) " +
                        "INNER JOIN VW_BRFYMXK b (nolock) ON a.syxh=b.syxh " +
                        "WHERE a.syxh = " + relationArg.HIS_SYXH + " AND a.xh = " + relationArg.HIS_JSXH + " " +
                        "AND b.zxrq >= a.ksrq AND b.zxrq < a.jzrq ";
            }

            sourceRemoteFeeDetailHub = await zyDbController.QueryAsync(query);
        }

        internal async Task PushFeeDetailInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (sourceRemoteFeeDetailHub.Count() != 0)
            {
                if (relationArg.UPLOAD_STAS == "1")
                {
                    query = "DELETE FROM FeeDetailRecord AS a WHERE a.SETL_BILL_ID='" + relationArg.SETL_BILL_ID + "'";

                    await nativedDbController.ExecuteAsync(query);
                }

                foreach (dynamic sourceRemoteFeeDetailRec in sourceRemoteFeeDetailHub)
                {
                    query = "INSERT INTO FeeDetailRecord (FEE_DETL_ID,SETL_BILL_ID," +
                            "FEE_OCUR_TIME,CNT,PRIC,DETITEM_FEE_SUMAMT," +
                            "MED_CHRGITM_TYPE,MEDLIST_NAME,MEDLIST_CODG,SPEC," +
                            "CHRGITM_LV,HOSP_APPR_FLAG) " +
                            "VALUES('" + Ulid.NewUlid().ToString() + "','" + relationArg.SETL_BILL_ID + "','" +
                            sourceRemoteFeeDetailRec.FEE_OCUR_TIME + "'," + sourceRemoteFeeDetailRec.CNT + "," + sourceRemoteFeeDetailRec.PRIC + "," + sourceRemoteFeeDetailRec.DETITEM_FEE_SUMAMT + ",'" +
                            sourceRemoteFeeDetailRec.MED_CHRGITM_TYPE + "','" + (sourceRemoteFeeDetailRec.MEDLIST_NAME as string).Replace("'", "''") + "','" + sourceRemoteFeeDetailRec.MEDLIST_CODG + "','" + sourceRemoteFeeDetailRec.SPEC + "','" +
                            sourceRemoteFeeDetailRec.CHRGITM_LV + "','" + sourceRemoteFeeDetailRec.HOSP_APPR_FLAG + "')";

                    await nativedDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FetchOprnInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            query = "WITH SS_SSDJK_ORDERLIST (id,ssxh) " +
                    "AS (SELECT ROW_NUMBER() OVER (ORDER BY a.ssdj,a.kssj) id,a.xh " +
                    "FROM SS_SSDJK a (nolock) WHERE a.jlzt = 2 AND a.syxh = " + relationArg.HIS_SYXH + ") " +
                    "SELECT b.ssmc 'OPRN_OPRT_NAME',b.ssdm 'OPRN_OPRT_CODE'," +
                    "CASE WHEN a.id = 1 THEN '1' ELSE '0' END MAIN_OPRN_FLAG," +
                    "dbo.fun_ConvertDateString(b.kssj,'DT') 'OPRN_BEGNTIME',dbo.fun_ConvertDateString(b.jssj,'DT') 'OPRN_ENDTIME'," +
                    "(SELECT x.ryxm FROM SS_SSRYK x(nolock) WHERE b.xh = x.ssxh AND x.rylb = '1') 'OPER_DR_NAME'," +
                    "(SELECT y.rydm FROM SS_SSRYK y(nolock) WHERE b.xh = y.ssxh AND y.rylb = '1') 'OPER_DR_CODE'," +
                    "(SELECT z.ryxm FROM SS_SSRYK z(nolock) WHERE b.xh = z.ssxh AND z.rylb = '8') 'ANST_DR_NAME'," +
                    "(SELECT w.rydm FROM SS_SSRYK w(nolock) WHERE b.xh = w.ssxh AND w.rylb = '8') 'ANST_DR_CODE'," +
                    "dbo.fun_ConvertDateString(b.mzkssj,'DT')  'ANST_BEGNTIME',dbo.fun_ConvertDateString(b.mzjssj,'DT') 'ANST_ENDTIME' " +
                    "FROM SS_SSDJK_ORDERLIST a (nolock) " +
                    "INNER JOIN SS_SSDJK b (nolock) ON a.ssxh=b.xh ";

            sourceRemoteOprnHub = await zyDbController.QueryAsync(query);
        }

        internal async Task PushOprnInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (sourceRemoteOprnHub.Count() != 0)
            {
                if (relationArg.UPLOAD_STAS == "1")
                {
                    query = "DELETE FROM OprnRecord AS a WHERE a.BILL_MDTRT_INFO_ID='" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await nativedDbController.ExecuteAsync(query);
                }

                foreach (dynamic sourceRemoteOprnRec in sourceRemoteOprnHub)
                {
                    query = "INSERT INTO OprnRecord (OPRN_INFO_ID,BILL_MDTRT_INFO_ID," +
                            "OPRN_OPRT_NAME,OPRN_OPRT_CODE,MAIN_OPRN_FLAG," +
                            "OPRN_BEGNTIME,OPRN_ENDTIME,OPER_DR_NAME,OPER_DR_CODE," +
                            "ANST_DR_NAME,ANST_DR_CODE,ANST_BEGNTIME,ANST_ENDTIME) " +
                            "VALUES('" + Ulid.NewUlid().ToString() + "','" + relationArg.BILL_MDTRT_INFO_ID + "','" +
                            sourceRemoteOprnRec.OPRN_OPRT_NAME + "','" + sourceRemoteOprnRec.OPRN_OPRT_CODE + "','" + sourceRemoteOprnRec.MAIN_OPRN_FLAG + "','" +
                            sourceRemoteOprnRec.OPRN_BEGNTIME + "','" + sourceRemoteOprnRec.OPRN_ENDTIME + "','" + sourceRemoteOprnRec.OPER_DR_NAME + "','" + sourceRemoteOprnRec.OPER_DR_CODE + "','" +
                            sourceRemoteOprnRec.ANST_DR_NAME + "','" + sourceRemoteOprnRec.ANST_DR_CODE + "','" + sourceRemoteOprnRec.ANST_BEGNTIME + "','" + sourceRemoteOprnRec.ANST_ENDTIME + "')";

                    await nativedDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FinishRelationInfoIntoNaviteRecord(RelationKind relationArg)
        {
            query = "UPDATE RelationRecord AS a SET UPLOAD_STAS = '2',UPDT_TIME = '" + DateTime.Now.ToString("G") + "' " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            await nativedDbController.ExecuteAsync(query);
        }
    }
}
