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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace BTLXLA
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CallPage : Page
    {
        public CallPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string passed = e.Parameter as string;
            if (passed.Equals(""))
                txtString.Text = "(╯°□°）╯︵ ┻━┻";
            else
                txtString.Text = passed;

            imgCapped.Source = MainPage.wb;
        }

        private void grdBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.wb = null;
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame != null && rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        private void grdCall_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI("VINAPHONE", "*101#");
        }
    }
}
