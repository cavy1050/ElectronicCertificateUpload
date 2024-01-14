using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Prism.Ioc;
using Prism.Mvvm;
using MaterialDesignThemes.Wpf;
using ElectronicCertificateUpload.Core;
using ElectronicCertificateUpload.Services;

namespace ElectronicCertificateUpload.Models
{
    public class MainWindowModel : BindableBase
    {
        IContainerProvider containerProvider;

        Timer timer, minimizeWindowTimer, closeWindowTimer;

        IGenerateController generateController;
        IUpLoadController upLoadController;

        ISnackbarMessageQueue messageQueue;
        public ISnackbarMessageQueue MessageQueue
        {
            get => messageQueue;
            set => SetProperty(ref messageQueue, value);
        }

        List<SelectiondKind> uploadRanges;
        public List<SelectiondKind> UploadRanges
        {
            get => uploadRanges;
            set => SetProperty(ref uploadRanges, value);
        }

        string currentUploadRange;
        public string CurrentUploadRange
        {
            get => currentUploadRange;
            set => SetProperty(ref currentUploadRange, value);
        }

        DateTime beginTime = DateTime.Now;
        public DateTime BeginTime
        {
            get => beginTime;
            set => SetProperty(ref beginTime, value);
        }

        DateTime endTime = DateTime.Now;
        public DateTime EndTime
        {
            get => endTime;
            set => SetProperty(ref endTime, value);
        }

        List<SelectiondKind> uploadTimePoints;
        public List<SelectiondKind> UploadTimePoints
        {
            get => uploadTimePoints;
            set => SetProperty(ref uploadTimePoints, value);
        }

        string currentUploadTimePoint;
        public string CurrentUploadTimePoint
        {
            get => currentUploadTimePoint;
            set
            {
                SetProperty(ref currentUploadTimePoint, value);
                NextUpLoadTime = DateTime.Now.Date.AddDays(1).AddHours(Convert.ToDouble(currentUploadTimePoint));
            }
        }

        int currentCount;
        public int CurrentCount
        {
            get => currentCount;
            set => SetProperty(ref currentCount, value);
        }

        int totalCount;
        public int TotalCount
        {
            get => totalCount;
            set => SetProperty(ref totalCount, value);
        }

        DateTime nextUpLoadTime;
        public DateTime NextUpLoadTime
        {
            get => nextUpLoadTime;
            set => SetProperty(ref nextUpLoadTime, value);
        }

        bool autoUpLoadFlag;
        public bool AutoUpLoadFlag
        {
            get => autoUpLoadFlag;
            set => SetProperty(ref autoUpLoadFlag, value);
        }

        bool selfFundedFlag;
        public bool SelfFundedFlag
        {
            get => selfFundedFlag;
            set => SetProperty(ref selfFundedFlag, value);
        }

        public MainWindowModel(IContainerProvider containerProviderArg)
        {
            containerProvider = containerProviderArg;
            MessageQueue = containerProviderArg.Resolve<ISnackbarMessageQueue>();
        }

        public void OnLoaded()
        {
            UploadRanges = Provider.UploadRanges;
            UploadTimePoints = Provider.UploadTimePoints;
        }

        public void OnMinimizeWindow(object window)
        {
            if (minimizeWindowTimer == null)
            {
                minimizeWindowTimer = new Timer(new TimerCallback(MinimizeWindowTimerTicked), window, 1000, -1);
            }
        }

        public void OnCloseWindow(object window)
        {
            if (closeWindowTimer == null)
            {
                closeWindowTimer = new Timer(new TimerCallback(CloseWindowTimerTicked), window, 1000, -1);
            }
        }

        private void MinimizeWindowTimerTicked(object obj)
        {
            Window window = obj as Window;
            window.Dispatcher.Invoke(new Action<object>(ExecMinimize),window);
        }

        private void ExecMinimize(object obj)
        {
            SystemCommands.MinimizeWindow(obj as Window);
        }

        private void CloseWindowTimerTicked(object obj)
        {
            Window window = obj as Window;
            window.Dispatcher.Invoke(new Action<object>(ExecClose), window);
        }

        private void ExecClose(object obj)
        {
            SystemCommands.CloseWindow(obj as Window);
        }

        public async void OnGenerate()
        {
            generateController = containerProvider.Resolve<IGenerateController>(CurrentUploadRange);
            generateController.Ticked += ControllerTicked;

            TotalCount = await generateController.CompareNativeFromSourceRemoteRecord(BeginTime, EndTime, SelfFundedFlag);

            if (TotalCount == 0)
                MessageQueue.Enqueue("没有数据可生成!");
            else
            {
                if (await generateController.SynchronizeNativeFromSourceRemoteRecord())
                    messageQueue.Enqueue("生成本地数据成功!");
            }
        }

        public async void OnUpLoad()
        {
            upLoadController = containerProvider.Resolve<IUpLoadController>(CurrentUploadRange);
            upLoadController.Ticked += ControllerTicked;

            TotalCount = await upLoadController.CompareNativeToTargetRemoteRecord(BeginTime, EndTime);

            if (TotalCount == 0)
                MessageQueue.Enqueue("没有数据可上传!");
            else
            {
                if (await upLoadController.SynchronizeNativeToTargetRemoteRecord())
                    messageQueue.Enqueue("上传本地数据成功!");
            }
        }

        public void OnStart()
        {
            if (!AutoUpLoadFlag)
            {
                AutoUpLoadFlag = true;

                TimeSpan nextUpLoadTimeSpan = new TimeSpan(NextUpLoadTime.Ticks);
                TimeSpan nextUpLoadTimeSpanInterval = nextUpLoadTimeSpan.Subtract(new TimeSpan(DateTime.Now.Ticks)).Duration();

                timer = new Timer(new TimerCallback(TimerTicked), null, nextUpLoadTimeSpanInterval, TimeSpan.FromDays(1));
            }
        }

        public void OnStop()
        {
            if (AutoUpLoadFlag)
            {
                AutoUpLoadFlag = false;

                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            }
        }

        private async void TimerTicked(object state)
        {
            //TODO 自动操作成功后需记录本地
            int generateCount, upLoadCount;
            bool generateRet = false;
            bool upLoadRet = false;

            generateController = containerProvider.Resolve<IGenerateController>(CurrentUploadRange);
            generateCount = await generateController.CompareNativeFromSourceRemoteRecord(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddDays(-1), SelfFundedFlag);

            if (generateCount != 0)
                generateRet = await generateController.SynchronizeNativeFromSourceRemoteRecord();

            upLoadController = containerProvider.Resolve<IUpLoadController>(CurrentUploadRange);
            upLoadCount = await upLoadController.CompareNativeToTargetRemoteRecord(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddDays(-1));

            if (upLoadCount != 0)
                upLoadRet = await upLoadController.SynchronizeNativeToTargetRemoteRecord();

            if (generateRet && upLoadRet)
                NextUpLoadTime = NextUpLoadTime.AddDays(1);
        }

        private void ControllerTicked(object sender, EventArgs e)
        {
            IController controller = sender as IController;
            CurrentCount = controller.CurrentCount;
        }
    }
}
