using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using LibBcore;

namespace Cica4WD.Models
{
    class CicaDriver : IDisposable
    {
        #region property

        private BcoreManager FrontBcore { get; set; }

        private BcoreManager RearBcore { get; set; }

        private bool IsRunning { get; set; }

        private static CoreDispatcher AppDispatcher => CoreApplication.MainView.CoreWindow.Dispatcher;

        #endregion

        #region event

        public event TypedEventHandler<CicaDriver, bool> BcoreConnectionChanged; 

        #endregion

        #region constructor

        public CicaDriver()
        {
            
        }

        ~CicaDriver()
        {
            Dispose();
        }

        #endregion

        #region method

        #region public

        public void Dispose()
        {
            FrontBcore?.Dispose();
            RearBcore?.Dispose();
        }

        public async void Start(DeviceInformation front, DeviceInformation rear)
        {
            FrontBcore?.Dispose();
            FrontBcore = await ConnectBcore(front);
            RearBcore?.Dispose();
            RearBcore = await ConnectBcore(rear);
            StartControllerTask();
        }

        public void Stop()
        {
        }

        #endregion

        private void StartControllerTask()
        {
            IsRunning = true;
            Task.Run(async () =>
            {
                var controller = App.Gamepads.FirstOrDefault();

                var lb = -1;
                var rb = -1;

                while (IsRunning && controller != null)
                {
                    var status = controller.GetCurrentReading();


                    var left = (int) (128*status.LeftThumbstickY + 128);
                    var right = (int) (128*status.RightThumbstickY + 128);

                    if (Math.Abs(left - 128) < 10) left = 128;
                    if (Math.Abs(right - 128) < 10) right = 128;

                    await AppDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (lb != left)
                        {
                            FrontBcore?.WriteMotorPwm(0, left, true);
                            RearBcore?.WriteMotorPwm(0, left);
                        }
                        if (rb != right)
                        {
                            FrontBcore?.WriteMotorPwm(1, right,true);
                            RearBcore?.WriteMotorPwm(1, right);
                        }
                    });

                    lb = left;
                    rb = right;
                    await Task.Delay(200);
                }
            });
        }

        private void OnConnectionChanged(object sender, bool isConnected)
        {
            var bcore = sender as BcoreManager;

            if (bcore == null) return;

            if (!isConnected)
            {
                Dispose();
            }

            if (bcore.DeviceName == FrontBcore?.DeviceName || bcore.DeviceName == RearBcore?.DeviceName)
            {
                BcoreConnectionChanged?.Invoke(this, isConnected);
            }
        }

        private async Task<BcoreManager> ConnectBcore(DeviceInformation info)
        {
            if (info == null) return null;

            var bcore = new BcoreManager(info);

            bcore.ConnectionChanged += OnConnectionChanged;

            var ret = await bcore.Init();

            return bcore;
        }

        #endregion
    }
}
