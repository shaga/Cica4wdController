using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cica4WD.Views
{
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (bool) value;

            return status ? "Connected" : "Not conntected";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class InitializeInfoView : UserControl
    {
        #region Dependency property

        public static readonly DependencyProperty IsConnectedGamepadProperty =
            DependencyProperty.Register(nameof(IsConnectedGamepad), typeof(bool), typeof(InitializeInfoView), new PropertyMetadata(false));

        public bool IsConnectedGamepad
        {
            get { return (bool)GetValue(IsConnectedGamepadProperty); }
            set { SetValue(IsConnectedGamepadProperty, value); }
        }

        public static readonly DependencyProperty IsConnectedFrontBcoreProperty =
            DependencyProperty.Register(nameof(IsConnectedFrontBcore), typeof(bool), typeof(InitializeInfoView), new PropertyMetadata(false));

        public bool IsConnectedFrontBcore
        {
            get { return (bool) GetValue(IsConnectedFrontBcoreProperty); }
            set { SetValue(IsConnectedFrontBcoreProperty, value); }
        }

        public static readonly DependencyProperty IsConnectedRearBcoreProperty =
            DependencyProperty.Register(nameof(IsConnectedRearBcore), typeof(bool), typeof(InitializeInfoView), new PropertyMetadata(false));

        public bool IsConnectedRearBcore
        {
            get { return (bool)GetValue(IsConnectedRearBcoreProperty); }
            set { SetValue(IsConnectedRearBcoreProperty, value); }
        }

        #endregion

        public InitializeInfoView()
        {
            this.InitializeComponent();
        }
    }
}
