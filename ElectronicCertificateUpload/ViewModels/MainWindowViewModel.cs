using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Commands;
using ElectronicCertificateUpload.Models;

namespace ElectronicCertificateUpload.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        MainWindowModel mainWindowModel;
        public MainWindowModel MainWindowModel
        {
            get => mainWindowModel;
            set => SetProperty(ref mainWindowModel, value);
        }

        public DelegateCommand<object> MinimizeWindowCommand { get; private set; }
        public DelegateCommand<object> CloseWindowCommand { get; private set; }
        public DelegateCommand LoadedCommand { get; private set; }
        public DelegateCommand GenerateCommand { get; private set; }
        public DelegateCommand UpLoadCommand { get; private set; }
        public DelegateCommand StartCommand { get; private set; }
        public DelegateCommand StopCommand { get; private set; }

        public MainWindowViewModel(IContainerProvider containerProviderArg)
        {
            MainWindowModel = new MainWindowModel(containerProviderArg);

            MinimizeWindowCommand = new DelegateCommand<object>(OnMinimizeWindow);
            CloseWindowCommand = new DelegateCommand<object>(OnCloseWindow);
            LoadedCommand = new DelegateCommand(OnLoaded);
            GenerateCommand = new DelegateCommand(OnGenerate);
            UpLoadCommand = new DelegateCommand(OnUpLoad);
            StartCommand= new DelegateCommand(OnStart);
            StopCommand = new DelegateCommand(OnStop);
        }

        private void OnMinimizeWindow(object window)
        {
            MainWindowModel.OnMinimizeWindow(window);
        }

        private void OnCloseWindow(object window)
        {
            MainWindowModel.OnCloseWindow(window);
        }

        private void OnLoaded()
        {
            MainWindowModel.OnLoaded();
        }

        private void OnGenerate()
        {
            MainWindowModel.OnGenerate();
        }

        private void OnUpLoad()
        {
            MainWindowModel.OnUpLoad();
        }

        private void OnStart()
        {
            mainWindowModel.OnStart();
        }

        private void OnStop()
        {
            mainWindowModel.OnStop();
        }
    }
}
