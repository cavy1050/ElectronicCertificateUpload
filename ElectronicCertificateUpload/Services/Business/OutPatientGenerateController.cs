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
    public class OutPatientGenerateController : IGenerateController
    {
        string query;

        IAsyncDbController nativedDbController, mzDbController;

        dynamic sourceRemoteMdtrtRec, soureRemoteBillMasterRec;
        IEnumerable<dynamic> sourceRemoteDiagHub, sourceRemoteFeeDetailHub;

        public IList<RelationKind> DifferenceHub { get; set; }

        public int CurrentCount { get; set; }

        public event EventHandler Ticked;

        public OutPatientGenerateController(IContainerProvider containerProviderArg)
        {
            nativedDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.NativeDB.ToString());
            mzDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.MZDB.ToString());

            DifferenceHub = new List<RelationKind>();
        }

        public async Task<int> CompareNativeFromSourceRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg, bool selfFundedFlagArg)
        {
            DateTime endAfterOneDayTime = endTimeArg.AddDays(1);

            query = "SELECT a.xh 'HIS_GHXH',b.sjh 'HIS_JSSJH',dbo.fun_ConvertDateString(b.sfrq, 'DT') 'BILL_TIME','0' 'SELF_FUNDED_FLAG','0' 'RHRED_FLAG' " +
                    "FROM dbo.VW_GHZDK a (nolock) " +
                    "INNER JOIN dbo.VW_MZBRJSK b (nolock) ON a.jssjh = b.ghsjh " +
                    "WHERE b.sfrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.sfrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                    "AND b.ybjszt = 2 " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_MZJZJLK x (nolock) WHERE b.sjh = x.sjh) " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_MZJSXXJLK y (nolock) WHERE b.sjh = y.sjh) " +
                    "AND EXISTS (SELECT 1 FROM YY_DZFP_MZFP z (nolock) WHERE b.sjh = z.sjh) " +
                    "UNION ALL " +
                    "SELECT a.xh 'HIS_GHXH',b.sjh 'HIS_JSSJH',dbo.fun_ConvertDateString(b.sfrq, 'DT') 'BILL_TIME','0' 'SELF_FUNDED_FLAG','1' 'RHRED_FLAG' " +
                    "FROM dbo.VW_GHZDK a (nolock) " +
                    "INNER JOIN dbo.VW_MZBRJSK b (nolock) ON a.jssjh = b.ghsjh " +
                    "WHERE b.sfrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.sfrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                    "AND b.ybjszt = 2 " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_MZJZJLK x (nolock) WHERE b.tsjh = x.sjh) " +
                    "AND EXISTS (SELECT 1 FROM YB_SI21_MZJSXXJLK_QXJS y (nolock) WHERE b.tsjh = y.sjh)  " +
                    "AND EXISTS (SELECT 1 FROM YY_DZFP_MZFP z (nolock) WHERE b.tsjh = z.sjh)";

            IEnumerable<RelationKind> sourcePatientHub = await mzDbController.QueryAsync<RelationKind>(query);

            if (selfFundedFlagArg)
            {
                query = "SELECT a.xh 'HIS_GHXH',b.sjh 'HIS_JSSJH',dbo.fun_ConvertDateString(b.sfrq, 'DT') 'BILL_TIME','1' 'SELF_FUNDED_FLAG',CASE WHEN b.jlzt=2 THEN '1' ELSE '0' END 'RHRED_FLAG' " +
                        "FROM dbo.VW_GHZDK a (nolock) " +
                        "INNER JOIN dbo.VW_MZBRJSK b (nolock) ON a.jssjh = b.ghsjh " +
                        "WHERE b.sfrq >= '" + beginTimeArg.ToString("yyyyMMdd") + "' AND b.sfrq < '" + endAfterOneDayTime.ToString("yyyyMMdd") + "' " +
                        "AND b.ybjszt = 2 AND b.ybdm = '001' " +
                        "AND EXISTS (SELECT 1 FROM YY_DZFP_MZFP z (nolock) WHERE b.sjh = z.sjh) ";

                IEnumerable<RelationKind> selfFundedPatientHub = await mzDbController.QueryAsync<RelationKind>(query);

                sourcePatientHub = selfFundedPatientHub == null ? sourcePatientHub : sourcePatientHub.Union(selfFundedPatientHub, new OutPatientGenerateRelationComparer());
            }

            query = "SELECT BILL_MDTRT_INFO_ID,SETL_BILL_ID,HIS_GHXH,HIS_JSSJH,BILL_TIME,RHRED_FLAG,SELF_FUNDED_FLAG FROM RelationRecord " +
                    "WHERE BILL_TIME >= '" + beginTimeArg.ToString("yyyy-MM-dd") + "' AND BILL_TIME < '" + endAfterOneDayTime.ToString("yyyy-MM-dd") + "' " +
                    "AND MDTRT_TYPE = '1' " +
                    "AND UPLOAD_STAS IN ('2','3','4')";

            IEnumerable<RelationKind> targetPatientHub = await nativedDbController.QueryAsync<RelationKind>(query);

            query = "SELECT BILL_MDTRT_INFO_ID,SETL_BILL_ID,HIS_GHXH,HIS_JSSJH,BILL_TIME,RHRED_FLAG,SELF_FUNDED_FLAG FROM RelationRecord " +
                    "WHERE BILL_TIME >= '" + beginTimeArg.ToString("yyyy-MM-dd") + "' AND BILL_TIME < '" + endAfterOneDayTime.ToString("yyyy-MM-dd") + "' " +
                    "AND MDTRT_TYPE = '1' " +
                    "AND UPLOAD_STAS = '1'";

            IEnumerable<RelationKind> generatedPatientHub = await nativedDbController.QueryAsync<RelationKind>(query);

            IEnumerable<RelationKind> comparedPatientHub = sourcePatientHub.Except(targetPatientHub, new OutPatientGenerateRelationComparer());

            DifferenceHub.Clear();

            foreach (RelationKind relation in comparedPatientHub)
            {
                if (generatedPatientHub.Contains(relation, new OutPatientGenerateRelationComparer()))
                    DifferenceHub.Add(new RelationKind
                    {
                        BILL_MDTRT_INFO_ID = generatedPatientHub.FirstOrDefault(x => x.HIS_JSSJH == relation.HIS_JSSJH).BILL_MDTRT_INFO_ID,
                        SETL_BILL_ID = generatedPatientHub.FirstOrDefault(x => x.HIS_JSSJH == relation.HIS_JSSJH).SETL_BILL_ID,
                        HIS_GHXH = relation.HIS_GHXH,
                        HIS_JSSJH = relation.HIS_JSSJH,
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
                        HIS_GHXH = relation.HIS_GHXH,
                        HIS_JSSJH = relation.HIS_JSSJH,
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
                    sourceRemoteMdtrtRec = soureRemoteBillMasterRec = sourceRemoteDiagHub = sourceRemoteFeeDetailHub = null;

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
        }

        internal async Task PushSourceRemoteIntoNaviteRecord(RelationKind relationArg)
        {
            await PushRelationInfoIntoNaviteRecord(relationArg);
            await PushMdtrtInfoIntoNaviteRecord(relationArg);
            await PushBillInfoIntoNaviteRecord(relationArg);
            await PushDiagInfoIntoNaviteRecord(relationArg);
            await PushFeeDetailInfoIntoNaviteRecord(relationArg);
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
                        relationArg.HIS_GHXH + ", '" + relationArg.HIS_JSSJH + "', 0, 0, '" +
                        relationArg.BILL_TIME + "', '1', '" + relationArg.RHRED_FLAG + "','" + relationArg.SELF_FUNDED_FLAG + "','" +
                        "1','" + DateTime.Now.ToString("G") + "') ";

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchMdtrtInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            if (relationArg.SELF_FUNDED_FLAG=="0")
            {
                query = "SELECT d.mdtrt_id 'BIZ_SN',c.sfzh 'CERT_NO',c.hzxm 'PSN_NAME',CASE WHEN c.sex='男' THEN '1' ELSE '2' END 'GEND'," +
                        "d.insutype 'INSUTYPE',d.psn_no 'HI_NO',d.insuplc_admdvs 'HI_ADMDVS_CODE',d.begntime 'BEGNTIME',d.begntime 'ENDTIME'," +
                        "d.med_type 'MED_TYPE',d.matn_type 'MATN_TYPE',d.birctrl_type 'BIRCTRL_TYPE'," +
                        "d.ipt_otp_no 'IPT_OTP_NO',c.blh 'MEDRCDNO',a.ysdm 'CHFPDR_CODE',x.name 'CHFPDR_NAME' " +
                        "FROM dbo.VW_GHZDK a(NOLOCK) " +
                        "INNER JOIN dbo.VW_MZBRJSK b (NOLOCK) ON a.jssjh = b.ghsjh " +
                        "INNER JOIN dbo.SF_BRXXK c (NOLOCK) ON a.patid = c.patid " +
                        "INNER JOIN dbo.YB_SI21_MZJZJLK d (NOLOCK) ON " + (relationArg.RHRED_FLAG == "0" ? "b.sjh" : "b.tsjh") + " = d.sjh " +
                        "LEFT  JOIN dbo.YY_ZGBMK x (NOLOCK) ON a.ysdm = x.id " +
                        "WHERE a.xh = " + relationArg.HIS_GHXH + " AND b.sjh='" + relationArg.HIS_JSSJH + "'";
            }
            else
            {
                query = "SELECT a.xh 'BIZ_SN',c.sfzh 'CERT_NO',c.hzxm 'PSN_NAME',CASE WHEN c.sex='男' THEN '1' ELSE '2' END 'GEND'," +
                        "'' 'INSUTYPE','' 'HI_NO','' 'HI_ADMDVS_CODE',dbo.fun_ConvertDateString(b.sfrq, 'DT') 'BEGNTIME',dbo.fun_ConvertDateString(b.sfrq, 'DT') 'ENDTIME'," +
                        "'' 'MED_TYPE','' 'MATN_TYPE','' 'BIRCTRL_TYPE','' 'FETTS'," +
                        "'' 'GESO_VAL','' 'FETUS_CNT','' 'DSCG_WAY','' 'DIE_DATE'," +
                        "'' 'IPT_OTP_NO','' 'MEDRCDNO',a.ysdm 'CHFPDR_CODE',x.name 'CHFPDR_NAME'," +
                        "'' 'ADM_CATY','' 'DSCG_CATY' " +
                        "FROM dbo.VW_GHZDK a (NOLOCK) " +
                        "INNER JOIN dbo.VW_MZBRJSK b (NOLOCK) ON a.jssjh = b.ghsjh " +
                        "INNER JOIN dbo.SF_BRXXK c (NOLOCK) ON a.patid = c.patid " +
                        "LEFT  JOIN dbo.YY_ZGBMK x (NOLOCK) ON a.ysdm = x.id " +
                        "WHERE a.xh = " + relationArg.HIS_GHXH + " AND b.sjh='" + relationArg.HIS_JSSJH + "'";
            }

            sourceRemoteMdtrtRec = await mzDbController.QueryFirstOrDefaultAsync(query);
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
                        "IPT_OTP_NO, MEDRCDNO, CHFPDR_CODE, CHFPDR_NAME) " +
                        "VALUES('" + relationArg.BILL_MDTRT_INFO_ID + "','" + sourceRemoteMdtrtRec.BIZ_SN + "','" +
                        sourceRemoteMdtrtRec.CERT_NO + "','" + sourceRemoteMdtrtRec.PSN_NAME + "','" + sourceRemoteMdtrtRec.GEND + "','" + sourceRemoteMdtrtRec.INSUTYPE + "','" + sourceRemoteMdtrtRec.HI_NO + "','" + sourceRemoteMdtrtRec.HI_ADMDVS_CODE + "','" +
                        sourceRemoteMdtrtRec.BEGNTIME + "','" + sourceRemoteMdtrtRec.ENDTIME + "','" + sourceRemoteMdtrtRec.MED_TYPE + "','" + sourceRemoteMdtrtRec.MATN_TYPE + "','" + sourceRemoteMdtrtRec.BIRCTRL_TYPE + "','" +
                        sourceRemoteMdtrtRec.IPT_OTP_NO + "','" + sourceRemoteMdtrtRec.MEDRCDNO + "','" + sourceRemoteMdtrtRec.CHFPDR_CODE + "','" + sourceRemoteMdtrtRec.CHFPDR_NAME + "')";

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchBillInfoFromSourceRemoteRecord(RelationKind relationArg)
        {
            if (relationArg.SELF_FUNDED_FLAG == "0")
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
                        "(SELECT x.pjdm FROM dbo.YY_DZFP_MZFP x (nolock) where x.xh=c.hcxh) 'REL_ELEC_SETL_CERT_CODE'," +
                        "(SELECT y.pjhm FROM dbo.YY_DZFP_MZFP y (nolock) where y.xh=c.hcxh) 'REL_ELEC_SETL_CERT_NO' " +
                        "FROM dbo.VW_MZBRJSK a (nolock) " +
                        "INNER JOIN dbo.YB_SI21_MZJSXXJLK b (nolock) ON " + (relationArg.RHRED_FLAG == "0" ? "a.sjh" : "a.tsjh") + "=b.sjh " +
                        "INNER JOIN dbo.YY_DZFP_MZFP c (nolock) on a.sjh=c.sjh " +
                        "WHERE a.sjh='" + relationArg.HIS_JSSJH + "'";
            }
            else
            {
                query = "SELECT '' 'SETL_ID',a.zje 'MEDFEE_AMT'," +
                        "0 'PSN_OWNPAY',0 'HIFP_PAY_AMT',0 'OTH_PAY',0 'PSN_ACCT_PAY',0 'PSN_CASH_PAY',0 'PSN_SELFPAY',0 'INSCP_AMT'," +
                        "c.pjdm 'BILL_CODE',c.pjhm 'BILL_NO',c.jym 'BILL_CHKCODE',c.kpd 'BILLER',c.kpd 'RECHKER'," +
                        "c.url 'FILE_URL'," +
                        "(SELECT x.pjdm FROM dbo.YY_DZFP_MZFP x (nolock) where x.xh=c.hcxh) 'REL_ELEC_SETL_CERT_CODE'," +
                        "(SELECT y.pjhm FROM dbo.YY_DZFP_MZFP y (nolock) where y.xh=c.hcxh) 'REL_ELEC_SETL_CERT_NO' " +
                        "FROM dbo.VW_MZBRJSK a (nolock) " +
                        "INNER JOIN dbo.YY_DZFP_MZFP c (nolock) on a.sjh=c.sjh " +
                        "WHERE a.sjh='" + relationArg.HIS_JSSJH + "'";
            }

            soureRemoteBillMasterRec = await mzDbController.QueryFirstOrDefaultAsync(query);
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
                    "a.zddm 'DIAG_CODE',a.zdmc 'DIAG_NAME',dbo.fun_ConvertDateString(a.lrrq, 'DT') 'DIAG_TIME'," +
                    "a.ysdm 'DIAG_DR_CODE',x.name 'DIAG_DR_NAME' " +
                    "FROM VW_MZBLZDK a (nolock) " +
                    "INNER JOIN YY_ZGBMK x (nolock) ON a.ysdm=x.id " +
                    "WHERE a.ghxh=" + relationArg.HIS_GHXH;

            sourceRemoteDiagHub = await mzDbController.QueryAsync(query);
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
            if (relationArg.SELF_FUNDED_FLAG == "0")
            {
                query = "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME',z.med_chrgitm_type 'MED_CHRGITM_TYPE'," +
                        "b.xmdm 'XMDM',b.xmmc 'XMMC','' 'SPEC',CONVERT(VARCHAR(50),'') 'MEDLIST_CODG',CONVERT(VARCHAR(50),'') 'MEDLIST_NAME'," +
                        "z.pric 'PRIC',z.cnt 'CNT',z.det_item_fee_sumamt 'DETITEM_FEE_SUMAMT',z.chrgitm_lv 'CHRGITM_LV' " +
                        "INTO #FEEDETLLIST " +
                        "FROM VW_GHZDK a (nolock) " +
                        "INNER JOIN VW_GHMXK b (nolock) ON a.xh=b.ghxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "INNER JOIN YB_SI21_MZJZJLK x (nolock) ON c.sjh=x.sjh " +
                        "INNER JOIN YB_SI21_MZJSXXJLK y (nolock) ON c.sjh=y.sjh " +
                        "INNER JOIN YB_SI21_MZCFJGJLK z (nolock) ON c.sjh=z.sjh AND b.xh=z.feedetl_sn " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "UNION ALL " +
                        "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME',z.med_chrgitm_type 'MED_CHRGITM_TYPE'," +
                        "b.xmdm 'XMDM',b.xmmc 'XMMC','' 'SPEC',CONVERT(VARCHAR(50),'') 'MEDLIST_CODG',CONVERT(VARCHAR(50),'') 'MEDLIST_NAME'," +
                        "z.pric 'PRIC',-z.cnt 'CNT',-z.det_item_fee_sumamt 'DETITEM_FEE_SUMAMT',z.chrgitm_lv 'CHRGITM_LV' " +
                        "FROM VW_GHZDK a (nolock) " +
                        "INNER JOIN VW_GHMXK b (nolock) ON a.txh=b.ghxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "INNER JOIN YB_SI21_MZJZJLK x (nolock) ON c.tsjh=x.sjh " +
                        "INNER JOIN YB_SI21_MZJSXXJLK_QXJS y (nolock) ON c.tsjh=y.sjh " +
                        "INNER JOIN YB_SI21_MZCFJGJLK z (nolock) ON c.tsjh=z.sjh AND b.xh=z.feedetl_sn " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "UNION ALL " +
                        "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME',z.med_chrgitm_type 'MED_CHRGITM_TYPE'," +
                        "b.ypdm 'XMDM',b.ypmc 'XMMC',b.ypgg 'SPEC',CONVERT(VARCHAR(50),'') 'MEDLIST_CODG',CONVERT(VARCHAR(50),'') 'MEDLIST_NAME'," +
                        "z.pric 'PRIC',z.cnt 'CNT',z.det_item_fee_sumamt 'DETITEM_FEE_SUMAMT',z.chrgitm_lv 'CHRGITM_LV' " +
                        "FROM VW_MZCFK a (nolock) " +
                        "INNER JOIN VW_MZCFMXK b (nolock) ON a.xh=b.cfxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "INNER JOIN YB_SI21_MZJZJLK x (nolock) ON c.sjh=x.sjh " +
                        "INNER JOIN YB_SI21_MZJSXXJLK y (nolock) ON c.sjh=y.sjh " +
                        "INNER JOIN YB_SI21_MZCFJGJLK z (nolock) ON c.sjh=z.sjh AND b.xh=z.feedetl_sn " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "UNION ALL " +
                        "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME',z.med_chrgitm_type 'MED_CHRGITM_TYPE'," +
                        "b.ypdm 'XMDM',b.ypmc 'XMMC',b.ypgg 'SPEC',CONVERT(VARCHAR(50),'') 'MEDLIST_CODG',CONVERT(VARCHAR(50),'') 'MEDLIST_NAME'," +
                        "z.pric 'PRIC',-z.cnt 'CNT',-z.det_item_fee_sumamt 'DETITEM_FEE_SUMAMT',z.chrgitm_lv 'CHRGITM_LV' " +
                        "FROM VW_MZCFK a (nolock) " +
                        "INNER JOIN VW_MZCFMXK b (nolock) ON a.xh=b.cfxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "INNER JOIN YB_SI21_MZJZJLK x (nolock) ON c.tsjh=x.sjh " +
                        "INNER JOIN YB_SI21_MZJSXXJLK_QXJS y (nolock) ON c.tsjh=y.sjh " +
                        "INNER JOIN YB_SI21_MZCFJGJLK z (nolock) ON c.tsjh=z.sjh AND b.tmxxh=z.feedetl_sn " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "UPDATE a SET a.MEDLIST_CODG=LEFT(c.hilist_code,50),a.MEDLIST_NAME=LEFT(c.hilist_name,25) " +
                        "FROM #FEEDETLLIST a " +
                        "INNER JOIN dbo.YK_YPCDMLK b (NOLOCK) ON a.XMDM=b.ypdm " +
                        "INNER JOIN dbo.YB_SI21_YBMLXXK c (NOLOCK) ON b.dydm_si21=c.hilist_code " +
                        "UPDATE a SET a.MEDLIST_CODG=LEFT(c.hilist_code,50),a.MEDLIST_NAME=LEFT(c.hilist_name,25) " +
                        "FROM #FEEDETLLIST a " +
                        "INNER JOIN dbo.YY_SFXXMK b (NOLOCK) ON a.XMDM=b.id " +
                        "INNER JOIN dbo.YB_SI21_YBMLXXK c (NOLOCK) ON b.dydm_si21=c.hilist_code " +
                        "SELECT FEE_OCUR_TIME,MED_CHRGITM_TYPE," +
                        "MEDLIST_CODG,MEDLIST_NAME,PRIC,CNT,SPEC,DETITEM_FEE_SUMAMT,CHRGITM_LV " +
                        "FROM #FEEDETLLIST WHERE DETITEM_FEE_SUMAMT != 0";
            }
            else
            {
                query = "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME','' 'MED_CHRGITM_TYPE',b.xmdm 'MEDLIST_CODG',b.xmmc 'MEDLIST_NAME'," +
                        "b.xmdj 'PRIC',b.xmsl 'CNT','' 'SPEC',CONVERT(NUMERIC(14,2),b.xmdj*b.xmsl) 'DETITEM_FEE_SUMAMT','' 'CHRGITM_LV' " +
                        "FROM VW_GHZDK a (nolock) " +
                        "INNER JOIN VW_GHMXK b (nolock) ON a.xh=b.ghxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "UNION ALL " +
                        "SELECT dbo.fun_ConvertDateString(c.sfrq, 'DT') 'FEE_OCUR_TIME','' 'MED_CHRGITM_TYPE',b.ypdm 'MEDLIST_CODG',b.ypmc 'MEDLIST_NAME'," +
                        "CONVERT(NUMERIC(14,2),(b.ylsj-b.yhdj)/b.ykxs) 'PRIC',sum(CONVERT(NUMERIC(14,2),b.ypsl*b.ts*b.cfts)) 'CNT',b.ypgg 'SPEC'," +
                        "sum(convert(numeric(14,2),(b.ylsj-b.yhdj)*b.ypsl*b.ts*b.cfts/b.ykxs)) 'DETITEM_FEE_SUMAMT','' 'CHRGITM_LV' " +
                        "FROM VW_MZCFK a (nolock) " +
                        "INNER JOIN VW_MZCFMXK b (nolock) ON a.xh=b.cfxh " +
                        "INNER JOIN VW_MZBRJSK c (nolock) ON a.jssjh=c.sjh " +
                        "WHERE c.sjh = '" + relationArg.HIS_JSSJH + "' " +
                        "GROUP BY dbo.fun_ConvertDateString(c.sfrq, 'DT'),b.ypdm,b.ypmc,b.ypgg,CONVERT(NUMERIC(14,2),(b.ylsj-b.yhdj)/b.ykxs)";
            }

            sourceRemoteFeeDetailHub = await mzDbController.QueryAsync(query);
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
                            "MED_CHRGITM_TYPE,MEDLIST_NAME,MEDLIST_CODG,SPEC,CHRGITM_LV) " +
                            "VALUES('" + Ulid.NewUlid().ToString() + "','" + relationArg.SETL_BILL_ID + "','" +
                            sourceRemoteFeeDetailRec.FEE_OCUR_TIME + "'," + sourceRemoteFeeDetailRec.CNT + "," + sourceRemoteFeeDetailRec.PRIC + "," + sourceRemoteFeeDetailRec.DETITEM_FEE_SUMAMT + ",'" +
                            sourceRemoteFeeDetailRec.MED_CHRGITM_TYPE + "','" + (sourceRemoteFeeDetailRec.MEDLIST_NAME as string).Replace("'", "''") + "','" + sourceRemoteFeeDetailRec.MEDLIST_CODG + "','" + sourceRemoteFeeDetailRec.SPEC + "','" + sourceRemoteFeeDetailRec.CHRGITM_LV + "')";

                    await nativedDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FinishRelationInfoIntoNaviteRecord(RelationKind relationArg)
        {
            query = "UPDATE RelationRecord AS a SET UPLOAD_STAS = '2',UPDT_TIME = '" + DateTime.Now.ToString("G") + "' "+
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            await nativedDbController.ExecuteAsync(query);
        }
    }
}
