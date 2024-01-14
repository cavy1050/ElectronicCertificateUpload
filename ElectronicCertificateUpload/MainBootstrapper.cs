using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Ioc;
using Prism.Unity;
using MaterialDesignThemes.Wpf;
using ElectronicCertificateUpload.Core;
using ElectronicCertificateUpload.Views;
using ElectronicCertificateUpload.Services;

namespace ElectronicCertificateUpload
{
    public class MainBootstrapper: PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistryArgs)
        {
            containerRegistryArgs.RegisterSingleton<ISnackbarMessageQueue, SnackbarMessageQueue>();
            containerRegistryArgs.RegisterScoped<ILogController, LogController>();

            containerRegistryArgs.Register<IAsyncDbController, NativeDbController>(DataBasePart.NativeDB.ToString());
            containerRegistryArgs.Register<IAsyncDbController, MZDbController>(DataBasePart.MZDB.ToString());
            containerRegistryArgs.Register<IAsyncDbController, ZYDbController>(DataBasePart.ZYDB.ToString());
            containerRegistryArgs.Register<IAsyncDbController, DZPJDbController>(DataBasePart.DZPJDB.ToString());

            containerRegistryArgs.Register<IGenerateController, OutPatientGenerateController>(RangePart.OutPatient.ToString());
            containerRegistryArgs.Register<IGenerateController, InHospitalGenerateController>(RangePart.InHospital.ToString());
            containerRegistryArgs.Register<IGenerateController, AllGenerateController>(RangePart.All.ToString());

            containerRegistryArgs.Register<IUpLoadController, OutPatientUpLoadController>(RangePart.OutPatient.ToString());
            containerRegistryArgs.Register<IUpLoadController, InHospitalUpLoadController>(RangePart.InHospital.ToString());
            containerRegistryArgs.Register<IUpLoadController, AllUpLoadController>(RangePart.All.ToString());


        }
    }
}
