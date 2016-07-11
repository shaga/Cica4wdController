using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
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
            IsRunning = false;
            FrontBcore?.Dispose();
            FrontBcore = null;
            RearBcore?.Dispose();
            RearBcore = null;
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

                    var speed = (int) (128*status.LeftThumbstickY);
                    var handle = status.RightThumbstickX;
                    var leftStickPush = status.Buttons.HasFlag(GamepadButtons.LeftThumbstick);
                    var leftButtonPush = status.Buttons.HasFlag(GamepadButtons.LeftShoulder);
                    var rightButtonPush = status.Buttons.HasFlag(GamepadButtons.RightShoulder);

                    int left = 0;
                    int right = 0;

                    if ((leftButtonPush || rightButtonPush) && speed == 128)
                    {
                        if (leftButtonPush)
                        {
                            FrontBcore?.WriteMotorPwmAsync(0, 0, true);
                            FrontBcore?.WriteMotorPwmAsync(1, 255, true);
                        }
                        else
                        {
                            FrontBcore?.WriteMotorPwmAsync(0, 255, true);
                            FrontBcore?.WriteMotorPwmAsync(1, 0, true);
                        }

                        RearBcore?.WriteMotorPwmAsync(0, 255);
                        RearBcore?.WriteMotorPwmAsync(1, 255);
                    }
                    else
                    {
                        if (leftStickPush)
                        {
                            left = (int) (128*handle + 128);
                            right = (int) (128*(0 - handle) + 128);
                        }
                        else
                        {
                            if (handle < 0)
                            {
                                right = speed + 128;
                                left = 128 + (int) (speed*(1 + handle*0.8));
                            }
                            else
                            {
                                left = speed + 128;
                                right = 128 + (int) (speed*(1 - handle*0.8));
                            }
                        }

                        if (Math.Abs(left - 128) < 10) left = 128;
                        if (Math.Abs(right - 128) < 10) right = 128;

                        await AppDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if (lb != left)
                            {
                                FrontBcore?.WriteMotorPwmAsync(0, left, true);
                                RearBcore?.WriteMotorPwmAsync(0, left, true);
                            }
                            if (rb != right)
                            {
                                FrontBcore?.WriteMotorPwmAsync(1, right);
                                RearBcore?.WriteMotorPwmAsync(1, right);
                            }
                        });

                        lb = left;
                        rb = right;
                    }
                    await Task.Delay(100);
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
