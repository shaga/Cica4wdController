using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;

namespace LibBcore
{
    public class BcoreFoundEventArgs : EventArgs
    {
        public ulong Address { get; }

        public DeviceInformation Bcore { get; }

        public string Name => Bcore?.Name;

        public string MacAddress
            => string.Join(":", Enumerable.Range(0, 6).Select(i => ((Address >> 8*(5 - i)) & 0xff).ToString("X2")));

        public BcoreFoundEventArgs(ulong addr, DeviceInformation device)
        {
            Address = addr;
            Bcore = device;
        }
    }

    public class BcoreScanner
    {
        #region property

        public bool IsScanning => Watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        private BluetoothLEAdvertisementWatcher Watcher { get; } = new BluetoothLEAdvertisementWatcher();

        private DeviceInformationCollection PairedBcores { get; set; }

        #endregion

        #region event

        public event EventHandler<BcoreFoundEventArgs> FoundBcore;
        public event EventHandler FinishedScan;

        #endregion

        #region constructor

        public BcoreScanner()
        {
            Watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(BcoreUuid.BcoreService);
            Watcher.Received += OnAdvertisemntReceived;
            Watcher.Stopped += (w, e) => FinishedScan?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region method

        #region public

        public async void StartScan()
        {
            await GetPairedBcore();

            Watcher.Start();
        }

        public void StopScan()
        {
            Watcher.Stop();
        }

        #endregion

        #region private

        private async Task GetPairedBcore()
        {
            PairedBcores = await DeviceInformation.FindAllAsync(BcoreUuid.BcoreServiceSelector);
        }

        private void OnAdvertisemntReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs e)
        {
            var address = $"_{e.BluetoothAddress.ToString("x12")}";

            var bcore = PairedBcores?.FirstOrDefault(i => i.Id.Contains(address));

            if (bcore == null) return;

            FoundBcore?.Invoke(this, new BcoreFoundEventArgs(e.BluetoothAddress, bcore));
        }

        #endregion

        #endregion
    }
}
