using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace Cica4WD.Models
{
    class GameController
    {
        private Gamepad Gamepad { get; set; }

        public GameController()
        {
            if (App.Gamepads.Any())
            {
                Gamepad = App.Gamepads.FirstOrDefault();
            }
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (Gamepad == null)
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    var reading = Gamepad.GetCurrentReading();

                    Debug.WriteLine($"left  x: ${reading.LeftThumbstickX}");
                    Debug.WriteLine($"left  y: ${reading.LeftThumbstickY}");
                    Debug.WriteLine($"right x: ${reading.RightThumbstickX}");
                    Debug.WriteLine($"right y: ${reading.RightThumbstickY}");

                    await Task.Delay(3000);
                }
            });
        }
    }
}
