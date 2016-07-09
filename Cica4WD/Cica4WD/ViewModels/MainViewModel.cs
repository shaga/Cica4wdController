using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Cica4WD.Models;
using LibBcore;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;

namespace Cica4WD.ViewModels
{
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
        #region field

        private BcoreFoundEventArgs _selectedBcore;
        private BcoreFoundEventArgs _frontBcore;
        private BcoreFoundEventArgs _rearBcore;
        private bool _isConnecting;
        private bool _isScanning;

        private DelegateCommand _commandScan;
        private DelegateCommand<string> _commandSelectBcore;
        private DelegateCommand<string> _commandResetBcore;
        private DelegateCommand _commandStartControl;

        #endregion

        #region property

        public BcoreFoundEventArgs SelectedBcore
        {
            get { return _selectedBcore; }
            set
            {
                if (_selectedBcore == value) return;
                if (value == null)
                {
                    
                }

                SetProperty(ref _selectedBcore, value);
                CommandSelectBcore.RaiseCanExecuteChanged();
            }
        }

        public BcoreFoundEventArgs FrontBcore
        {
            get { return _frontBcore; }
            set
            {
                SetProperty(ref _frontBcore, value); 
                OnPropertyChanged(nameof(FrontBcoreName));
                CommandSelectBcore.RaiseCanExecuteChanged();
                CommandResetBcore.RaiseCanExecuteChanged();
                CommandStartControl.RaiseCanExecuteChanged();
            }
        }

        public string FrontBcoreName => FrontBcore?.Name ?? "---";

        public BcoreFoundEventArgs RearBcore
        {
            get { return _rearBcore; }
            set
            {
                SetProperty(ref _rearBcore, value); 
                OnPropertyChanged(nameof(RearBcoreName));
                CommandSelectBcore.RaiseCanExecuteChanged();
                CommandResetBcore.RaiseCanExecuteChanged();
                CommandStartControl.RaiseCanExecuteChanged();
            }
        }

        public string RearBcoreName => RearBcore?.Name ?? "---";

        public bool IsScanning
        {
            get { return _isScanning; }
            set
            {
                SetProperty(ref _isScanning, value);
                CommandSelectBcore.RaiseCanExecuteChanged();
                CommandStartControl.RaiseCanExecuteChanged();
            }
        }

        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                SetProperty(ref _isConnecting, value);
                CommandStartControl.RaiseCanExecuteChanged();
                CommandSelectBcore.RaiseCanExecuteChanged();
                CommandSelectBcore.RaiseCanExecuteChanged();
                CommandResetBcore.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<BcoreFoundEventArgs> FoundBcores { get; } = new ObservableCollection<BcoreFoundEventArgs>(); 

        private BcoreScanner Scanner { get; }

        private static CoreDispatcher AppDispatcher => CoreApplication.MainView.CoreWindow.Dispatcher;

        private CicaDriver Driver { get; set; }

        #region Command

        public DelegateCommand CommandScan
        {
            get { return _commandScan ?? (_commandScan = new DelegateCommand(Scan, () => !IsConnecting)); }
        }


        public DelegateCommand<string> CommandSelectBcore
        {
            get
            {
                return _commandSelectBcore ??
                       (_commandSelectBcore = new DelegateCommand<string>(SelectBcore, CanSelectBcore));
            }
        }

        public DelegateCommand<string> CommandResetBcore
        {
            get
            {
                return _commandResetBcore ??
                       (_commandResetBcore = new DelegateCommand<string>(ResetBcore, CanRestBcore));
            }
        }

        public DelegateCommand CommandStartControl
        {
            get { return _commandStartControl ?? (_commandStartControl = new DelegateCommand(StartControl, CanStartControl)); }
        }

        #endregion

        #endregion

        #region constructor

        public MainViewModel()
        {
            Scanner = new BcoreScanner();
            Scanner.FinishedScan += OnFinishedScan;
            Scanner.FoundBcore += OnFoundBcore;
        }

        #endregion

        #region method

        #region private

        #region Scan

        private void Scan()
        {
            if (IsScanning) Scanner.StopScan();
            else
            {
                FoundBcores.Clear();
                FrontBcore = null;
                RearBcore = null;
                Scanner.StartScan();
                IsScanning = true;
            }
        }

        private void OnFoundBcore(object sender, BcoreFoundEventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (FoundBcores.Any(i => i.Address == e.Address)) return;

                FoundBcores.Add(e);
            });
        }

        private void OnFinishedScan(object sender, EventArgs e)
        {
            RunOnUiThread(() => IsScanning = false);
        }

        #endregion

        #region Select

        private void SelectBcore(string param)
        {
            if (SelectedBcore == null) return;

            var isFront = ParamToBoolean(param);

            if (isFront)
            {
                if (SelectedBcore.Address != (RearBcore?.Address ?? 0))
                {
                    FrontBcore = SelectedBcore;
                }
            }
            else
            {
                if (SelectedBcore.Address != (FrontBcore?.Address ?? 0))
                {
                    RearBcore = SelectedBcore;
                }
            }
        }

        private bool CanSelectBcore(string param)
        {
            if (IsScanning || IsConnecting || SelectedBcore == null) return false;

            var isFront = ParamToBoolean(param);

            if ((isFront && SelectedBcore.Address == (RearBcore?.Address ?? 0)) ||
                (!isFront && SelectedBcore.Address == (FrontBcore?.Address ?? 0))) return false;

            return true;
        }

        private void ResetBcore(string param)
        {
            var isFront = ParamToBoolean(param);

            if (isFront && FrontBcore != null)
            {
                FrontBcore = null;
            }
            else if (!isFront && RearBcore != null)
            {
                RearBcore = null;
            }
        }

        private bool CanRestBcore(string param)
        {
            var isFront = ParamToBoolean(param);

            return (isFront && FrontBcore != null) || (!isFront && RearBcore != null);
        }

        private static bool ParamToBoolean(string param)
        {
            if (string.IsNullOrEmpty(param)) return true;

            return param.ToLower() != "false";
        }

        #endregion

        private void StartControl()
        {
            if (Driver == null)
            {
                Driver = new CicaDriver();
                Driver.BcoreConnectionChanged += DisconnectedDriver;
                Driver.Start(FrontBcore.Bcore, RearBcore.Bcore);
                IsConnecting = true;
            }
            else
            {
                Driver.BcoreConnectionChanged -= DisconnectedDriver;
                Driver.Stop();
                Driver = null;
                IsConnecting = false;
            }

            //var driver = new CicaDriver();
            //driver.BcoreConnectionChanged += (d, isConnected) =>
            //{

            //};
            //driver.Start(FrontBcore.Bcore, RearBcore?.Bcore);
        }

        private void DisconnectedDriver(CicaDriver driver, bool isConnected)
        {
            Driver.BcoreConnectionChanged -= DisconnectedDriver;
            Driver = null;
        }

        private bool CanStartControl()
        {
            return FrontBcore != null && RearBcore != null && !IsScanning;
        }

        #region RunOnUiThread

        private static async void RunOnUiThread(Action action)
        {
            await AppDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        #endregion

        #endregion

        #endregion
    }
}
