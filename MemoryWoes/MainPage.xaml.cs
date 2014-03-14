// -----------------------------------------------------------------------
//  <copyright file="MainPage.xaml.cs" company="Henric Jungheim">
//  Copyright (c) 2012-2014.
//  <author>Henric Jungheim</author>
//  </copyright>
// -----------------------------------------------------------------------
// Copyright (c) 2012-2014 Henric Jungheim <software@henric.org>
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;

namespace MemoryWoes
{
    public static class ByteConversion
    {
        const double ToMiB = 1.0 / (1024 * 1024);

        public static double BytesToMiB(this long value)
        {
            return value * ToMiB;
        }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        long _totalBytesRead;
        readonly DispatcherTimer _statusPoll;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

            _statusPoll = new DispatcherTimer
                          {
                              Interval = TimeSpan.FromMilliseconds(300)
                          };

            _statusPoll.Tick += (sender, args) =>
                                {
                                    download.Text = string.Format("{0:F2} MiB downloaded", Interlocked.Read(ref _totalBytesRead).BytesToMiB());

                                    var gcMemory = GC.GetTotalMemory(true).BytesToMiB();

                                    status.Text = string.Format("GC {0:F2} MiB App {1:F2}/{2:F2}/{3:F2} MiB", gcMemory,
                                        DeviceStatus.ApplicationCurrentMemoryUsage.BytesToMiB(),
                                        DeviceStatus.ApplicationPeakMemoryUsage.BytesToMiB(),
                                        DeviceStatus.ApplicationMemoryUsageLimit.BytesToMiB());
                                };
        }

        async Task DoSomethingWithData(byte[] data, int offset, int length)
        {
            // Simulate consuming the data at ~1MBit/s
            await Task.Delay((int)(length * (8.0 / 1000) + 0.5)).ConfigureAwait(false);

            // Blocking here by waiting for the delay to complete does NOT help.
            //Task.Delay((int) (length * (8.0 / 1000) + 0.5)).Wait();
        }

        async Task Download()
        {
            var buffer = new byte[4096];

            using (var httpClient = new HttpClient())
            {
                using (var stream = await httpClient.GetStreamAsync("http://dds.cr.usgs.gov/pub/data/nationalatlas/elev48i0100a.tif_nt00828.tar.gz").ConfigureAwait(false))
                {
                    for (;;)
                    {
                        var length = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                        if (length < 1)
                            return;

                        Interlocked.Add(ref _totalBytesRead, length);

                        await DoSomethingWithData(buffer, 0, length).ConfigureAwait(false);
                    }
                }
            }
        }

        async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                message.Text = "Reading";

                await Download();

                message.Text = "Done";
            }
            catch (Exception ex)
            {
                message.Text = "Failed: " + ex.Message;
            }
        }

        void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            _statusPoll.Start();
        }

        void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _statusPoll.Stop();
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}
