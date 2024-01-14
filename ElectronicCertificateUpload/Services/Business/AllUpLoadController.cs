using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    public class AllUpLoadController : IUpLoadController
    {
        IUpLoadController outPatientUpLoadController, inHospitalUpLoadController;
        int outPatientCount, inHospitalCount;
        bool outPatientRet, inHospitalRet;

        public int CurrentCount { get; set; }

        public event EventHandler Ticked;

        public AllUpLoadController(IContainerProvider containerProviderArg)
        {
            outPatientUpLoadController = containerProviderArg.Resolve<IUpLoadController>(RangePart.OutPatient.ToString());
            inHospitalUpLoadController = containerProviderArg.Resolve<IUpLoadController>(RangePart.InHospital.ToString());
            outPatientUpLoadController.Ticked += UpLoadController_Ticked;
            inHospitalUpLoadController.Ticked += UpLoadController_Ticked;
        }

        public async Task<int> CompareNativeToTargetRemoteRecord(DateTime beginTimeArg, DateTime endTimeArg)
        {
            outPatientCount = inHospitalCount = 0;

            outPatientCount = await outPatientUpLoadController.CompareNativeToTargetRemoteRecord(beginTimeArg, endTimeArg);
            inHospitalCount = await inHospitalUpLoadController.CompareNativeToTargetRemoteRecord(beginTimeArg, endTimeArg);

            return outPatientCount + inHospitalCount;
        }

        public async Task<bool> SynchronizeNativeToTargetRemoteRecord()
        {
            outPatientRet = inHospitalRet = false;

            if (outPatientCount != 0)
                outPatientRet = await outPatientUpLoadController.SynchronizeNativeToTargetRemoteRecord();

            if (inHospitalCount != 0)
                inHospitalRet = await inHospitalUpLoadController.SynchronizeNativeToTargetRemoteRecord();

            return outPatientRet || inHospitalRet;
        }

        private void UpLoadController_Ticked(object sender, EventArgs e)
        {
            CurrentCount = outPatientUpLoadController.CurrentCount + inHospitalUpLoadController.CurrentCount;

            Ticked?.Invoke(this, new EventArgs());
        }
    }
}
