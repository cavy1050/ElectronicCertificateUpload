using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    public class InHospitalUpLoadController : IUpLoadController
    {
        string query;

        IAsyncDbController nativedDbController, dzpjDbController;

        dynamic nativeMdtrtRec, nativeBillMasterRec, nativeBillStasRec, nativeBillRltsRec;
        IEnumerable<dynamic> nativeDiagHub, nativeOprnHub, nativeFeeDetailHub;

        public IList<RelationKind> DifferenceHub { get; set; }

        public event EventHandler Ticked;

        public int CurrentCount { get; set; }

        public InHospitalUpLoadController(IContainerProvider containerProviderArg)
        {
            nativedDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.NativeDB.ToString());
            dzpjDbController = containerProviderArg.Resolve<IAsyncDbController>(DataBasePart.DZPJDB.ToString());

            DifferenceHub = new List<RelationKind>();
        }

        public async Task<int> CompareNativeToTargetRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg)
        {
            DateTime endAfterOneDayTime = endTimeArg.AddDays(1);

            query = "SELECT BILL_MDTRT_INFO_ID,SETL_BILL_ID,HIS_SYXH,HIS_JSXH," +
                    "BILL_TIME,RHRED_FLAG,UPLOAD_STAS,SELF_FUNDED_FLAG " +
                    "FROM RelationRecord " +
                    "WHERE BILL_TIME >= '" + beginTimeArg.ToString("yyyy-MM-dd") + "' AND BILL_TIME < '" + endAfterOneDayTime.ToString("yyyy-MM-dd") + "' " +
                    "AND MDTRT_TYPE = '2' " +
                    "AND UPLOAD_STAS IN ('2','3')";

            IEnumerable<RelationKind> sourcePatientHub = await nativedDbController.QueryAsync<RelationKind>(query);

            DifferenceHub.Clear();
            DifferenceHub = sourcePatientHub.ToList();

            return DifferenceHub.Count();
        }

        public async Task<bool> SynchronizeNativeToTargetRemoteRecord()
        {
            bool ret = false;

            if (DifferenceHub.Count != 0)
            {
                for (CurrentCount = 1; CurrentCount <= DifferenceHub.Count; CurrentCount++)
                {
                    nativeMdtrtRec = nativeBillMasterRec = nativeDiagHub = nativeOprnHub = nativeFeeDetailHub = null;

                    await FetchTargetRemoteFromNativeRecord(DifferenceHub.ElementAt(CurrentCount - 1));

                    await PushNaviteIntoTargetRemoteRecord(DifferenceHub.ElementAt(CurrentCount - 1));

                    Ticked?.Invoke(this, new EventArgs());
                }

                ret = true;
            }

            CurrentCount = CurrentCount <= DifferenceHub.Count ? CurrentCount : DifferenceHub.Count;

            return ret;
        }

        internal async Task FetchTargetRemoteFromNativeRecord(RelationKind relationArg)
        {
            await FetchMdtrtInfoFromNativeRecord(relationArg);
            await FetchBillInfoFromNativeRecord(relationArg);
            await FetchDiagInfoFromNativeRecord(relationArg);
            await FetchFeeDetailInfoFromNativeRecord(relationArg);
            await FetchOprnInfoFromNativeRecord(relationArg);
            await FetchBillStasInfoFromNativeRecord(relationArg);
            await FetchBillRltsInfoFromNativeRecord(relationArg);
        }

        internal async Task PushNaviteIntoTargetRemoteRecord(RelationKind relationArg)
        {
            await PushMdtrtInfoIntoTargetRemoteRecord(relationArg);
            await RefreshRelationInfoIntoNaviteRecord(relationArg);
            await PushBillInfoIntoTargetRemoteRecord(relationArg);
            await PushDiagInfoIntoTargetRemoteRecord(relationArg);
            await PushFeeDetailInfoIntoTargetRemoteRecord(relationArg);
            await PushOprnInfoIntoTargetRemoteRecord(relationArg);
            await PushBillStasInfoIntoTargetRemoteRecord(relationArg);
            await PushBillRltsInfoIntoTargetRemoteRecord(relationArg);
            await FinishRelationInfoIntoNaviteRecord(relationArg);
        }

        internal async Task FetchMdtrtInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT a.BILL_MDTRT_INFO_ID,a.SETL_BILL_ID,c.BILL_CODE,c.BILL_NO,c.BILL_CHKCODE," +
                    "a.MDTRT_TYPE,a.RHRED_FLAG 'ELEC_SETL_CERT_FLAG'," +
                    "c.REL_ELEC_SETL_CERT_CODE,c.REL_ELEC_SETL_CERT_NO," +
                    "b.CERT_NO,b.PSN_NAME,b.GEND,c.BILLER,c.RECHKER,c.MEDFEE_AMT 'BILL_AMT'," +
                    "SUBSTR(a.BILL_TIME, 1, 4) || SUBSTR(a.BILL_TIME, 6, 2) || SUBSTR(a.BILL_TIME, 9, 2) 'BILL_DATE'," +
                    "SUBSTR(a.BILL_TIME, 12, 8) 'BILL_TIME',b.INSUTYPE,b.HI_NO," +
                    "c.PSN_OWNPAY,c.MEDFEE_AMT,c.HIFP_PAY_AMT,c.OTH_PAY,c.PSN_ACCT_PAY,c.PSN_CASH_PAY,c.PSN_SELFPAY," +
                    "b.BEGNTIME,b.ENDTIME,b.MED_TYPE,b.MATN_TYPE,b.BIRCTRL_TYPE," +
                    "b.FETTS,b.GESO_VAL,b.FETUS_CNT,b.DSCG_WAY,b.DIE_DATE," +
                    "b.IPT_OTP_NO,b.MEDRCDNO,c.SETL_ID,b.CHFPDR_CODE,b.CHFPDR_NAME," +
                    "b.ADM_CATY,b.DSCG_CATY " +
                    "FROM RelationRecord a " +
                    "INNER JOIN MdtrtRecord b ON a.BILL_MDTRT_INFO_ID = b.BILL_MDTRT_INFO_ID " +
                    "INNER JOIN BillMasterRecord c ON a.SETL_BILL_ID = c.SETL_BILL_ID " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' " +
                    "AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            nativeMdtrtRec = await nativedDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushMdtrtInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeMdtrtRec != null)
            {
                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_bill_mdtrt_info a " +
                            "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO mid_bsw_bill_mdtrt_info" +
                        "(BILL_MDTRT_INFO_ID, SETL_BILL_ID, BILL_CODE, BILL_NO, BILL_CHKCODE," +
                        "BILL_TYPE, SETL_TYPE, MDTRT_TYPE, ELEC_SETL_CERT_FLAG, REL_ELEC_SETL_CERT_CODE," +
                        "REL_ELEC_SETL_CERT_NO, SUPNINS_CODE," +
                        "CERT_NO, PSN_NAME, GEND, BILLER, RECHKER," +
                        "BILL_AMT, BILL_DATE, BILL_TIME, INSUTYPE, HI_NO," +
                        "PSN_OWNPAY, MEDFEE_AMT, HIFP_PAY_AMT, OTH_PAY, PSN_ACCT_PAY, PSN_CASH_PAY, PSN_SELFPAY," +
                        "FIXMEDINS_CODE, FIXMEDINS_NAME, FIX_BLNG_ADMDVS, BEGNTIME, ENDTIME," +
                        "MED_TYPE, MATN_TYPE, BIRCTRL_TYPE, FETTS, GESO_VAL," +
                        "FETUS_CNT, DSCG_WAY, DIE_DATE, IPT_OTP_NO, MEDRCDNO," +
                        "SETL_ID, CHFPDR_CODE, CHFPDR_NAME, ADM_CATY, DSCG_CATY, CRTE_TIME) " +
                        "VALUES('" + relationArg.BILL_MDTRT_INFO_ID + "', '" + relationArg.SETL_BILL_ID + "','" + nativeMdtrtRec.BILL_CODE + "','" + nativeMdtrtRec.BILL_NO + "','" + nativeMdtrtRec.BILL_CHKCODE + "','" +
                        "1','" + (relationArg.SELF_FUNDED_FLAG == "0" ? "1" : "2") + "','" + nativeMdtrtRec.MDTRT_TYPE + "', '" + nativeMdtrtRec.ELEC_SETL_CERT_FLAG + "','" + nativeMdtrtRec.REL_ELEC_SETL_CERT_CODE + "','" +
                        nativeMdtrtRec.REL_ELEC_SETL_CERT_NO + "', '" + Provider.SUPNINS_CODE + "', '" +
                        nativeMdtrtRec.CERT_NO + "', '" + nativeMdtrtRec.PSN_NAME + "', '" + nativeMdtrtRec.GEND + "', '" + nativeMdtrtRec.BILLER + "', '" + nativeMdtrtRec.RECHKER + "', " +
                        nativeMdtrtRec.BILL_AMT + ", '" + nativeMdtrtRec.BILL_DATE + "', '" + nativeMdtrtRec.BILL_TIME + "', '" + nativeMdtrtRec.INSUTYPE + "', '" + nativeMdtrtRec.HI_NO + "', " +
                        nativeMdtrtRec.PSN_OWNPAY + ", " + nativeMdtrtRec.MEDFEE_AMT + ", " + nativeMdtrtRec.HIFP_PAY_AMT + ", " + nativeMdtrtRec.OTH_PAY + ", " + nativeMdtrtRec.PSN_ACCT_PAY + ", " + nativeMdtrtRec.PSN_CASH_PAY + ", " + nativeMdtrtRec.PSN_SELFPAY + ", '" +
                        Provider.FIXMEDINS_CODE + "', '" + Provider.FIXMEDINS_NAME + "', '" + Provider.FIX_BLNG_ADMDVS + "', '" + nativeMdtrtRec.BEGNTIME + "', '" + nativeMdtrtRec.ENDTIME + "', '" +
                        nativeMdtrtRec.MED_TYPE + "', '" + nativeMdtrtRec.MATN_TYPE + "', '" + nativeMdtrtRec.BIRCTRL_TYPE + "', " + (string.IsNullOrEmpty(nativeMdtrtRec.FETTS) ? "0" : nativeMdtrtRec.FETTS) + ", " + (string.IsNullOrEmpty(nativeMdtrtRec.GESO_VAL) ? "0" : nativeMdtrtRec.GESO_VAL) + ", " +
                        (string.IsNullOrEmpty(nativeMdtrtRec.FETUS_CNT) ? "0" : nativeMdtrtRec.FETUS_CNT) + ", '" + nativeMdtrtRec.DSCG_WAY + "', " + (nativeMdtrtRec.DIE_DATE == "" ? "NULL" : "'" + nativeMdtrtRec.DIE_DATE + "'") + ", '" + nativeMdtrtRec.IPT_OTP_NO + "', '" + nativeMdtrtRec.MEDRCDNO + "', '" +
                        nativeMdtrtRec.SETL_ID + "', '" + nativeMdtrtRec.CHFPDR_CODE + "', '" + nativeMdtrtRec.CHFPDR_NAME + "', '" + nativeMdtrtRec.ADM_CATY + "', '" + nativeMdtrtRec.DSCG_CATY + "', '" + DateTime.Now.ToString("G") + "') ";

                await dzpjDbController.ExecuteAsync(query);
            }
        }

        internal async Task RefreshRelationInfoIntoNaviteRecord(RelationKind relationArg)
        {
            if (relationArg.UPLOAD_STAS == "2")
            {
                query = "UPDATE RelationRecord AS a SET UPLOAD_STAS = '3',UPDT_TIME = '" + DateTime.Now.ToString("G") + "' " +
                        "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

                await nativedDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchBillInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT a.SETL_BILL_ID,a.BILL_CODE,BILL_NO,a.FILE_URL " +
                    "FROM BillMasterRecord a " +
                    "WHERE a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            nativeBillMasterRec = await nativedDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushBillInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeBillMasterRec != null)
            {
                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_setl_bill a " +
                            "WHERE a.SETL_BILL_ID = '" + nativeMdtrtRec.SETL_BILL_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO mid_bsw_setl_bill" +
                        "(SETL_BILL_ID, BILL_CODE, BILL_NO, FILE_URL, READ_STAS," +
                        "FIXMEDINS_CODE, REPT_UPLD_FLAG, UPDT_TIME, CRTE_TIME) " +
                        "VALUES('" + relationArg.SETL_BILL_ID + "', '" + nativeBillMasterRec.BILL_CODE + "','" + nativeBillMasterRec.BILL_NO + "','" + nativeBillMasterRec.FILE_URL + "',0,'" +
                        Provider.FIXMEDINS_CODE + "',0,'" + DateTime.Now.ToString("G") + "', '" + DateTime.Now.ToString("G") + "') ";

                await dzpjDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchDiagInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT c.DIAG_INFO_ID,a.BILL_MDTRT_INFO_ID," +
                    "b.BILL_CODE,b.BILL_NO," +
                    "c.DIAG_TYPE,c.MAINDIAG_FLAG,c.DIAG_CODE,c.DIAG_NAME," +
                    "c.DIAG_TIME,c.DIAG_DR_CODE,c.DIAG_DR_NAME " +
                    "FROM  RelationRecord a " +
                    "INNER JOIN BillMasterRecord b ON a.SETL_BILL_ID = b.SETL_BILL_ID " +
                    "INNER JOIN DiagRecord c ON a.BILL_MDTRT_INFO_ID = c.BILL_MDTRT_INFO_ID " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

            nativeDiagHub = await nativedDbController.QueryAsync(query);
        }

        internal async Task PushDiagInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeDiagHub.Count() != 0)
            {
                query = "UPDATE mid_bsw_bill_mdtrt_info a SET a.DIAG_INFO_COUNT = " + nativeDiagHub.Count().ToString() + " " +
                        "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                await dzpjDbController.ExecuteAsync(query);

                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_diag_info a " +
                            "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                foreach (dynamic nativeDiagRec in nativeDiagHub)
                {
                    query = "INSERT INTO mid_bsw_diag_info " +
                            "(DIAG_INFO_ID, BILL_MDTRT_INFO_ID, BILL_CODE, BILL_NO," +
                            "DIAG_TYPE, MAINDIAG_FLAG, DIAG_CODE, DIAG_NAME," +
                            "DIAG_TIME, DIAG_DR_CODE, DIAG_DR_NAME, CRTE_TIME) " +
                            "VALUES('" + nativeDiagRec.DIAG_INFO_ID + "','" + nativeDiagRec.BILL_MDTRT_INFO_ID + "','" + nativeDiagRec.BILL_CODE + "','" + nativeDiagRec.BILL_NO + "','" +
                            nativeDiagRec.DIAG_TYPE + "','" + nativeDiagRec.MAINDIAG_FLAG + "','" + nativeDiagRec.DIAG_CODE + "','" + nativeDiagRec.DIAG_NAME + "','" +
                            nativeDiagRec.DIAG_TIME + "','" + nativeDiagRec.DIAG_DR_CODE + "','" + nativeDiagRec.DIAG_DR_NAME + "','" + DateTime.Now.ToString("G") + "') ";

                    await dzpjDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FetchFeeDetailInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT c.FEE_DETL_ID,a.BILL_MDTRT_INFO_ID," +
                    "b.BILL_CODE,b.BILL_NO," +
                    "c.FEE_OCUR_TIME,c.CNT,c.PRIC,c.DETITEM_FEE_SUMAMT," +
                    "c.MED_CHRGITM_TYPE,c.MEDLIST_NAME,c.MEDLIST_CODG,c.SPEC," +
                    "c.CHRGITM_LV,c.HOSP_APPR_FLAG " +
                    "FROM  RelationRecord a " +
                    "INNER JOIN BillMasterRecord b ON a.SETL_BILL_ID = b.SETL_BILL_ID " +
                    "INNER JOIN FeeDetailRecord c ON a.SETL_BILL_ID = c.SETL_BILL_ID " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' " +
                    "AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            nativeFeeDetailHub = await nativedDbController.QueryAsync(query);
        }

        internal async Task PushFeeDetailInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeFeeDetailHub.Count() != 0)
            {
                query = "UPDATE mid_bsw_bill_mdtrt_info a SET a.FEE_DETL_COUNT=" + nativeFeeDetailHub.Count().ToString() + " " +
                        "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                await dzpjDbController.ExecuteAsync(query);

                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_fee_detl a " +
                            "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                foreach (dynamic nativeFeeDetailRec in nativeFeeDetailHub)
                {
                    query = "INSERT INTO mid_bsw_fee_detl " +
                            "(FEE_DETL_ID, BILL_MDTRT_INFO_ID, BILL_CODE, BILL_NO," +
                            "FEE_OCUR_TIME, CNT, PRIC, DETITEM_FEE_SUMAMT, MED_CHRGITM_TYPE," +
                            "MEDLIST_NAME, MEDLIST_CODG, SPEC, CHRGITM_LV, HOSP_APPR_FLAG, CRTE_TIME) " +
                            "VALUES('" + nativeFeeDetailRec.FEE_DETL_ID + "','" + nativeFeeDetailRec.BILL_MDTRT_INFO_ID + "','" + nativeFeeDetailRec.BILL_CODE + "','" + nativeFeeDetailRec.BILL_NO + "','" +
                            nativeFeeDetailRec.FEE_OCUR_TIME + "'," + nativeFeeDetailRec.CNT + "," + nativeFeeDetailRec.PRIC + "," + nativeFeeDetailRec.DETITEM_FEE_SUMAMT + ",'" + nativeFeeDetailRec.MED_CHRGITM_TYPE + "','" +
                            (nativeFeeDetailRec.MEDLIST_NAME as string).Replace("'", "''") + "','" + nativeFeeDetailRec.MEDLIST_CODG + "','" + nativeFeeDetailRec.SPEC + "','" + nativeFeeDetailRec.CHRGITM_LV + "','" + nativeFeeDetailRec.HOSP_APPR_FLAG + "','" + DateTime.Now.ToString("G") + "') ";

                    await dzpjDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FetchOprnInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT c.OPRN_INFO_ID,a.BILL_MDTRT_INFO_ID," +
                    "b.BILL_CODE,b.BILL_NO," +
                    "c.OPRN_OPRT_NAME,c.OPRN_OPRT_CODE,c.MAIN_OPRN_FLAG,c.OPRN_BEGNTIME,c.OPRN_ENDTIME," +
                    "c.OPER_DR_NAME,c.OPER_DR_CODE,c.ANST_DR_NAME,c.ANST_DR_CODE,c.ANST_BEGNTIME,c.ANST_ENDTIME " +
                    "FROM  RelationRecord a " +
                    "INNER JOIN BillMasterRecord b ON a.SETL_BILL_ID = b.SETL_BILL_ID " +
                    "INNER JOIN OprnRecord c ON a.BILL_MDTRT_INFO_ID = c.BILL_MDTRT_INFO_ID " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

            nativeOprnHub = await nativedDbController.QueryAsync(query);
        }

        internal async Task PushOprnInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeOprnHub.Count() != 0)
            {
                query = "UPDATE mid_bsw_bill_mdtrt_info a SET a.OPRN_INFO_COUNT = " + nativeOprnHub.Count().ToString() + " " +
                        "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                await dzpjDbController.ExecuteAsync(query);

                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_oprn_info a " +
                            "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                foreach (dynamic nativeOprnRec in nativeOprnHub)
                {
                    query = "INSERT INTO mid_bsw_oprn_info " +
                            "(OPRN_INFO_ID, BILL_MDTRT_INFO_ID, BILL_CODE, BILL_NO," +
                            "OPRN_OPRT_NAME, OPRN_OPRT_CODE, MAIN_OPRN_FLAG, OPRN_BEGNTIME, OPRN_ENDTIME," +
                            "OPER_DR_NAME, OPER_DR_CODE, ANST_DR_NAME, ANST_DR_CODE, ANST_BEGNTIME, ANST_ENDTIME, CRTE_TIME) " +
                            "VALUES('" + nativeOprnRec.OPRN_INFO_ID + "','" + nativeOprnRec.BILL_MDTRT_INFO_ID + "','" + nativeOprnRec.BILL_CODE + "','" + nativeOprnRec.BILL_NO + "','" +
                            nativeOprnRec.OPRN_OPRT_NAME + "','" + nativeOprnRec.OPRN_OPRT_CODE + "','" + nativeOprnRec.MAIN_OPRN_FLAG + "','" + nativeOprnRec.OPRN_BEGNTIME + "','" + nativeOprnRec.OPRN_ENDTIME + "','" +
                            nativeOprnRec.OPER_DR_NAME + "','" + nativeOprnRec.OPER_DR_CODE + "','" + nativeOprnRec.ANST_DR_NAME + "','" + nativeOprnRec.ANST_DR_CODE + "','" + nativeOprnRec.ANST_BEGNTIME + "','" + nativeOprnRec.ANST_ENDTIME + "','" + DateTime.Now.ToString("G") + "') ";

                    await dzpjDbController.ExecuteAsync(query);
                }
            }
        }

        internal async Task FetchBillStasInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT a.BILL_STAS_ID,a.BILL_CODE,a.BILL_NO,a.BILL_CHKCODE " +
                    "FROM BillMasterRecord a " +
                    "WHERE a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            nativeBillStasRec = await nativedDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushBillStasInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeBillStasRec != null)
            {
                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_setl_bill_stas a " +
                            "WHERE a.BILL_STAS_ID = '" + nativeBillStasRec.BILL_STAS_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO mid_bsw_setl_bill_stas" +
                        "(BILL_STAS_ID, BILL_CODE, BILL_NO, BILL_CHKCODE, RHRED_FLAG," +
                        "REIM_FLAG, QURY_RSTL_FLAG, FIXMEDINS_CODE, UPDT_TIME, CRTE_TIME) " +
                        "VALUES('" + nativeBillStasRec.BILL_STAS_ID + "', '" + nativeBillStasRec.BILL_CODE + "','" + nativeBillStasRec.BILL_NO + "','" + nativeBillStasRec.BILL_CHKCODE + "','" + relationArg.RHRED_FLAG + "'," +
                        "'1','0','" + Provider.FIXMEDINS_CODE + "', '" + DateTime.Now.ToString("G") + "','" + DateTime.Now.ToString("G") + "') ";

                await dzpjDbController.ExecuteAsync(query);
            }
        }

        internal async Task FetchBillRltsInfoFromNativeRecord(RelationKind relationArg)
        {
            query = "SELECT c.SETL_RLTS_ID,c.BILL_CODE,c.BILL_NO,c.BILL_CHKCODE," +
                    "a.BILL_TIME,c.MEDFEE_AMT,b.HI_ADMDVS_CODE,b.BIZ_SN, " +
                    "c.SETL_ID,b.CERT_NO,a.BILL_TIME 'SETL_DATE',b.HI_NO 'PSN_NO'," +
                    "b.PSN_NAME,b.INSUTYPE 'INSU_TYPE',c.INSCP_AMT,c.HIFP_PAY_AMT," +
                    "(SELECT DIAG_CODE FROM DiagRecord w WHERE w.BILL_MDTRT_INFO_ID = a.BILL_MDTRT_INFO_ID AND w.MAINDIAG_FLAG = '1') 'MAINDIAG_CODE'," +
                    "(SELECT DIAG_NAME FROM DiagRecord x WHERE x.BILL_MDTRT_INFO_ID = a.BILL_MDTRT_INFO_ID AND x.MAINDIAG_FLAG = '1') 'MAINDIAG_NAME'," +
                    "(SELECT DIAG_CODE FROM DiagRecord y WHERE y.BILL_MDTRT_INFO_ID = a.BILL_MDTRT_INFO_ID AND y.MAINDIAG_FLAG != '1') 'SCDDIAG_CODE'," +
                    "(SELECT DIAG_NAME FROM DiagRecord z WHERE z.BILL_MDTRT_INFO_ID = a.BILL_MDTRT_INFO_ID AND z.MAINDIAG_FLAG != '1') 'SCDDIAG_NAME' " +
                    "FROM RelationRecord a " +
                    "INNER JOIN MdtrtRecord b ON a.BILL_MDTRT_INFO_ID=b.BILL_MDTRT_INFO_ID " +
                    "INNER JOIN BillMasterRecord c ON a.SETL_BILL_ID=c.SETL_BILL_ID " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' " +
                    "AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            nativeBillRltsRec = await nativedDbController.QueryFirstOrDefaultAsync(query);
        }

        internal async Task PushBillRltsInfoIntoTargetRemoteRecord(RelationKind relationArg)
        {
            if (nativeBillRltsRec != null)
            {
                if (relationArg.UPLOAD_STAS == "3")
                {
                    query = "DELETE a FROM mid_bsw_setl_rlts a " +
                            "WHERE a.SETL_RLTS_ID = '" + nativeBillRltsRec.SETL_RLTS_ID + "'";

                    await dzpjDbController.ExecuteAsync(query);
                }

                query = "INSERT INTO mid_bsw_setl_rlts" +
                        "(SETL_RLTS_ID, BILL_CODE, BILL_NO, BILL_CHKCODE, BLNG_ADMDVS_CODE," +
                        "BILL_TYPE, SETL_TYPE, MED_TYPE, BILL_DATE, MEDFEE_AMT," +
                        "HI_ADMDVS_CODE, BIZ_SN, SETL_ID, CERT_NO, SETL_DATE," +
                        "PSN_NO, PSN_NAME, INSU_TYPE, INSCP_AMT, HIFP_PAY_AMT," +
                        "MAINDIAG_CODE, MAINDIAG_NAME, SCDDIAG_CODE, SCDDIAG_NAME," +
                        "READ_STAS, REPT_UPLD_FLAG, UPDT_TIME, CRTE_TIME) " +
                        "VALUES('" + nativeBillRltsRec.SETL_RLTS_ID + "', '" + nativeBillRltsRec.BILL_CODE + "','" + nativeBillRltsRec.BILL_NO + "','" + nativeBillRltsRec.BILL_CHKCODE + "','" + Provider.FIX_BLNG_ADMDVS + "','" +
                        "1','" + (relationArg.SELF_FUNDED_FLAG == "0" ? "1" : "2") + "','" + nativeMdtrtRec.MDTRT_TYPE + "', '" + nativeBillRltsRec.BILL_TIME + "', " + nativeBillRltsRec.MEDFEE_AMT + ", '" +
                        nativeBillRltsRec.HI_ADMDVS_CODE + "', '" + nativeBillRltsRec.BIZ_SN + "', '" + nativeBillRltsRec.SETL_ID + "', '" + nativeBillRltsRec.CERT_NO + "', '" + nativeBillRltsRec.SETL_DATE + "', '" +
                        nativeBillRltsRec.PSN_NO + "', '" + nativeBillRltsRec.PSN_NAME + "', '" + nativeBillRltsRec.INSU_TYPE + "', " + nativeBillRltsRec.INSCP_AMT + ", " + nativeBillRltsRec.HIFP_PAY_AMT + ", '" +
                        nativeBillRltsRec.MAINDIAG_CODE + "', '" + nativeBillRltsRec.MAINDIAG_NAME + "', '" + nativeBillRltsRec.SCDDIAG_CODE + "', '" + nativeBillRltsRec.SCDDIAG_NAME + "', " +
                        "'0','0','" + DateTime.Now.ToString("G") + "','" + DateTime.Now.ToString("G") + "') ";

                await dzpjDbController.ExecuteAsync(query);
            }
        }

        internal async Task FinishRelationInfoIntoNaviteRecord(RelationKind relationArg)
        {
            query = "UPDATE RelationRecord AS a SET UPLOAD_STAS = '4',UPDT_TIME = '" + DateTime.Now.ToString("G") + "' " +
                    "WHERE a.BILL_MDTRT_INFO_ID = '" + relationArg.BILL_MDTRT_INFO_ID + "' AND a.SETL_BILL_ID = '" + relationArg.SETL_BILL_ID + "'";

            await nativedDbController.ExecuteAsync(query);
        }
    }
}
