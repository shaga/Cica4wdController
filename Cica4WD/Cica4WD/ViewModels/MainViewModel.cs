using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Sensors;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Cica4WD.Models;
using LibBcore;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;

namespace Cica4WD.ViewModels
{
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (bool)value;

            return status ? "Connected" : "Not conntected";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class ButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = (bool) value;
            var prefix = parameter as string;

            return prefix + " " + (val ? "Stop" : "Start");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class MainViewModel : BindableBase
    {
        #region const

        private const string BcoreFrontName = "bCore_D05FB84E8A30";
        private const string BcoreRearName = "bCore_D05FB84E7F32";

        private enum ETurnMode
        {
            None,
            Left,
            Right,
        }

        public enum EControlMode
        {
            Car,
            Tank,
        }

        #endregion

        #region field

        private bool _isConnectedGamepad;

        private EControlMode _controlMode;

        private BcoreManager _frontBcoreManager;
        private BcoreManager _rearBcoreManager;

        #endregion

        #region property

        public bool IsConnectedGamepad
        {
            get { return _isConnectedGamepad; }
            set { SetProperty(ref _isConnectedGamepad, value); }
        }

        private BcoreManager FrontBcoreManager
        {
            get { return _frontBcoreManager; }
            set
            {
                _frontBcoreManager = value; 
                OnPropertyChanged(nameof(IsConnectedFrontBcore));
            }
        }

        private BcoreManager RearBcoreManager
        {
            get { return _rearBcoreManager; }
            set
            {
                _rearBcoreManager = value; 
                OnPropertyChanged(nameof(IsConnectedRearBcore));
            }
        }

        public EControlMode ControlMode
        {
            get { return _controlMode; }
            set { SetProperty(ref _controlMode, value); }
        }

        private ETurnMode TurnMode { get; set; }

        private GamepadReader GamepadReader { get; }

        public bool IsConnectedFrontBcore => FrontBcoreManager != null;
        public bool IsConnectedRearBcore => RearBcoreManager != null;

        private BcoreScanner Scanner { get; }

        private static CoreDispatcher AppDispatcher => CoreApplication.MainView.CoreWindow.Dispatcher;

        private int BeforeSpeedLeft { get; set; } = 128;

        private int BeforeSpeedRight { get; set; } = 128;

        private bool IsPressBack { get; set; }

        #endregion

        #region constructor

        public MainViewModel()
        {
            GamepadReader = new GamepadReader();
            GamepadReader.ConnectedGamepad += (s, isConnected) =>
            {
                RunOnUiThread(() =>
                {
                    IsConnectedGamepad = isConnected;
                    if (!isConnected)
                    {
                        FrontBcoreManager?.WriteMotorPwm(0, Bcore.StopMotorPwm);
                        FrontBcoreManager?.WriteMotorPwm(1, Bcore.StopMotorPwm);
                        RearBcoreManager?.WriteMotorPwm(0, Bcore.StopMotorPwm);
                        RearBcoreManager?.WriteMotorPwm(1, Bcore.StopMotorPwm);
                    }
                });
            };
            GamepadReader.UpdatedGamepadStatus += OnUpdatedGamepadStatus;

            GamepadReader.Start();

            Scanner = new BcoreScanner();
            Scanner.FoundBcore += OnFoundBcore;

            Scanner.StartScan();
        }

        #endregion

        #region method

        #region private

        #region Scan

        private void OnFoundBcore(object sender, BcoreFoundEventArgs e)
        {
            RunOnUiThread(async () =>
            {


                switch (e.Name)
                {
                    case BcoreFrontName:
                        FrontBcoreManager = new BcoreManager(e.Bcore);
                        await FrontBcoreManager.Init();
                        break;
                    case BcoreRearName:
                        RearBcoreManager = new BcoreManager(e.Bcore);
                        await RearBcoreManager.Init();
                        break;
                    default:
                        return;
                }

                if (FrontBcoreManager == null || RearBcoreManager == null)
                {
                    return;
                }

                Scanner.StopScan();
            });
        }

        #endregion

        #endregion


        #region RunOnUiThread

        private static async void RunOnUiThread(Action action)
        {
            await AppDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        #endregion

        #endregion

        private void OnUpdatedGamepadStatus(object sender, GamepadReading status)
        {
            if (status.Buttons.HasFlag(GamepadButtons.Menu))
            {
                RunOnUiThread(() =>
                {
                    FrontBcoreManager?.Dispose();
                    FrontBcoreManager = null;
                    RearBcoreManager?.Dispose();
                    RearBcoreManager = null;
                    if (!Scanner.IsScanning)
                    {
                        Scanner.StartScan();
                    }
                });
            }

            if (!IsPressBack && status.Buttons.HasFlag(GamepadButtons.View))
            {
                IsPressBack = true;
                RunOnUiThread(() =>
                {
                    if (ControlMode == EControlMode.Car) ControlMode = EControlMode.Tank;
                    else ControlMode = EControlMode.Car;
                });
            }
            else if (!status.Buttons.HasFlag(GamepadButtons.View))
            {
                IsPressBack = false;
            }

            if (FrontBcoreManager == null || RearBcoreManager == null) return;

            if (TurnMode != ETurnMode.Left && status.Buttons.HasFlag(GamepadButtons.LeftShoulder) && !status.Buttons.HasFlag(GamepadButtons.RightShoulder))
            {
                TurnMode = ETurnMode.Left;
            }
            else if (TurnMode != ETurnMode.Right && status.Buttons.HasFlag(GamepadButtons.RightShoulder) && !status.Buttons.HasFlag(GamepadButtons.LeftShoulder))
            {
                TurnMode = ETurnMode.Right;
            }
            else if (!status.Buttons.HasFlag(GamepadButtons.LeftShoulder) && !status.Buttons.HasFlag(GamepadButtons.RightShoulder))
            {
                TurnMode = ETurnMode.None;
            }

            if (TurnMode != ETurnMode.None)
            {
                SetTurnSpeed(TurnMode);
                return;
            }

            if (ControlMode == EControlMode.Tank)
            {
                SetTankSpeed(status);
            }
            else
            {
                SetCarSpeed(status);
            }
        }

        private void SetCarSpeed(GamepadReading status)
        {
            var speed = (int)(128 * status.LeftThumbstickY);
            var handle = status.RightThumbstickX;

            var left = 0;
            var right = 0;

            if (handle < 0)
            {
                right = speed + 128;
                left = 128 + (int)(speed * (1 + handle * 0.8));
            }
            else
            {
                left = speed + 128;
                right = 128 + (int)(speed * (1 - handle * 0.8));
            }

            SetMotorSpeed(left, right);
        }

        private void SetTankSpeed(GamepadReading status)
        {
            var left = (int) (128*status.LeftThumbstickY) + 128;
            var right = (int) (128*status.RightThumbstickY) + 128;

            SetMotorSpeed(left, right);
        }

        private void SetTurnSpeed(ETurnMode mode)
        {
            var left = 128;
            var right = 128;

            if (mode == ETurnMode.Left)
            {
                left -= 96;
                right += 96;
            }
            else if (mode == ETurnMode.Right)
            {
                left += 96;
                right -= 96;
            }
            else
            {
                return;
            }

            SetMotorSpeed(left, right);
        }

        private void SetMotorSpeed(int left, int right)
        {
            if (Math.Abs(left - 128) < 10) left = 128;
            if (Math.Abs(right - 128) < 10) right = 128;

            RunOnUiThread(() =>
            {
                if (left != BeforeSpeedLeft)
                {
                    FrontBcoreManager.WriteMotorPwm(0, left, true);
                    RearBcoreManager.WriteMotorPwm(0, left, true);
                }
                if (right != BeforeSpeedRight)
                {
                    FrontBcoreManager.WriteMotorPwm(1, right);
                    RearBcoreManager.WriteMotorPwm(1, right);
                }

                BeforeSpeedLeft = left;
                BeforeSpeedRight = right;
            });
        }
    }
}
