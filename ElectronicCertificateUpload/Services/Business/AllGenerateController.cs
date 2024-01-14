using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    public class AllGenerateController : IGenerateController
    {
        IGenerateController outPatientGenerateController, inHospitalGenerateController;
        int outPatientCount,inHospitalCount;
        bool outPatientRet, inHospitalRet;

        public int CurrentCount { get; set; }

        public event EventHandler Ticked;

        public AllGenerateController(IContainerProvider containerProviderArg)
        {
            outPatientGenerateController = containerProviderArg.Resolve<IGenerateController>(RangePart.OutPatient.ToString());
            inHospitalGenerateController = containerProviderArg.Resolve<IGenerateController>(RangePart.InHospital.ToString());
            outPatientGenerateController.Ticked += GenerateController_Ticked;
            inHospitalGenerateController.Ticked += GenerateController_Ticked;
        }

        public async Task<int> CompareNativeFromSourceRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg, bool selfFundedFlagArg)
        {
            //占位返回 return await new Task<int>(new Func<int>(() => { return 0; }));
            outPatientCount = inHospitalCount = 0;

            outPatientCount = await outPatientGenerateController.CompareNativeFromSourceRemoteRecord(beginTimeArg, endTimeArg, selfFundedFlagArg);
            inHospitalCount = await inHospitalGenerateController.CompareNativeFromSourceRemoteRecord(beginTimeArg, endTimeArg, selfFundedFlagArg);

            return outPatientCount + inHospitalCount;
        }

        public async Task<bool> SynchronizeNativeFromSourceRemoteRecord()
        {
            //占位返回 return await new Task<bool>(new Func<bool>(() => { return false; }));
            outPatientRet = inHospitalRet = false;

            if (outPatientCount != 0)
                outPatientRet = await outPatientGenerateController.SynchronizeNativeFromSourceRemoteRecord();

            if (inHospitalCount != 0)
                inHospitalRet = await inHospitalGenerateController.SynchronizeNativeFromSourceRemoteRecord();

            return outPatientRet || inHospitalRet;
        }

        private void GenerateController_Ticked(object sender, EventArgs e)
        {
            CurrentCount = outPatientGenerateController.CurrentCount + inHospitalGenerateController.CurrentCount;

            Ticked?.Invoke(this, new EventArgs());
        }
    }
}
