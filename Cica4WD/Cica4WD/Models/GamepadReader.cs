using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace Cica4WD.Models
{
    class GamepadReader
    {


        #region property

        private Gamepad UsingGamepad { get; set; }

        #endregion

        #region event

        public event EventHandler NotifyNoGamepad;

        public event EventHandler<bool> ConnectedGamepad;

        public event EventHandler<GamepadReading> UpdatedGamepadStatus;

        #endregion

        #region method

        public void Start()
        {
            App.AddedGamepad += OnAddedGamepad;
            App.RemovedGamepad += OnRemovedGamepad;

            UsingGamepad = App.Gamepads.FirstOrDefault();

            if (UsingGamepad != null)
            {
                ConnectedGamepad?.Invoke(this, true);
                Task.Run(() => ReadGamepadState());
            }
        }

        private async void ReadGamepadState()
        {
            while (UsingGamepad != null)
            {
                var status = UsingGamepad.GetCurrentReading();

                UpdatedGamepadStatus?.Invoke(this, status);

                await Task.Delay(80);
            }
        }

        private void OnAddedGamepad(object sender, Gamepad gamepad)
        {
            if (UsingGamepad != null) return;
            UsingGamepad = gamepad;
            ConnectedGamepad?.Invoke(this, true);
            Task.Run(() => ReadGamepadState());
        }

        private void OnRemovedGamepad(object sender, Gamepad gamepad)
        {
            if (UsingGamepad == null || UsingGamepad.GetHashCode() != gamepad.GetHashCode()) return;

            UsingGamepad = null;
            ConnectedGamepad?.Invoke(this, false);
        }

        #endregion
    }
}
