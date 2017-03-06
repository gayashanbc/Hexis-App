using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Hexis_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BluetoothPage : Page
    {
        // Let's Define what we'll use throughout our App
        StreamSocket BTSock; // Socket used to Communicate with the Arduino/BT Module
        /* This is used for our Speech App
         * SpeechRecognizer mySpeech; // Used for Recognizing App Speech (not yet in this App)
         * SpeechSynthesizer mySpeechSS = new SpeechSynthesizer();
         * 
         * This is used to Grab Content from the Net, we'll be using GZIP in the end
         * WebClient wc = new WebClient(); // Setting up our WebClient so we can just use "wc" and could be used to get an API
        */

        // Let's Store our Strings
        string BTStatus = ""; // Used to Store if we can send Message (e.g. yes or no)
        /*string BT_Received = ""; // We'll use to store Bluetooth Received Data
        string whattosay = ""; // Used later to accept input for Speech */
        
        // Constructor

        public BluetoothPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded; // We need Async, so Use _Loaded
            

        }
      
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            PeerFinder.Start();
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = ""; // Find/Get All Paired BT Devices
            var peers = await PeerFinder.FindAllPeersAsync(); // Make peers the container for All BT Devices
            txtBTStatus.Text = "Finding Paired Devices..."; // Tell UI what is going on in case it's Slow

            // Only want 1 Device to Show? Uncomment Below
            // lstBTPaired.Items.Add(peers[0].DisplayName); // 1 Paired Device to Show 

            // Show only Specific Device
            // peers[0].DisplayName.Contains("RN42-5");

            // Let's show only the first 2 Devices Paired
            for (int i = 0; i < peers.Count; i++)
            {
                lstBTPaired.Items.Add(peers[i].DisplayName);
            }
            if (peers.Count <= 2)
            {
                txtBTStatus.Text = "Found " + peers.Count + " Devices";
            }

        }

        private async void BT2Arduino_Send(string WhatToSend)
        {
            if (BTSock == null) // If we don't have a connection, Send Error Control
            {
                // MessageBox.Show("Please connect to a device first."); // Alert the user with a Notification (Optional)
                txtBTStatus.Text = "No connection found. Try again!"; // Alert the UI
                return; // Stop
            }
            else
                if (BTSock != null) // Since we have a Connection
            {
                var datab = GetBufferFromByteArray(UTF8Encoding.UTF8.GetBytes(WhatToSend)); // Create Buffer/Packet for Sending
                await BTSock.OutputStream.WriteAsync(datab); // Send our Message to Connected Arduino
                txtBTStatus.Text = "Message Sent (" + WhatToSend + ")"; // Show what we sent to Device to UI
                
            }
        }

        private async void BT2Arduino_Receive()
        {
            if (BTSock == null) // If we don't have a connection, Send Error Control
            {
                // MessageBox.Show("Please connect to a device first."); // Alert the user with a Notification (Optional)
                txtBTStatus.Text = "No connection found. Try again!"; // Alert the UI
                return; // Stop
            }
            else
                if (BTSock != null) // Since we have a Connection
            {
                // var datab = GetBufferFromByteArray(UTF8Encoding.UTF8.GetBytes(WhatToSend)); // Create Buffer/Packet for Sending
                //await BTSock.OutputStream.WriteAsync(datab); // Send our Message to Connected Arduino
                while (BTSock != null)
                {
                    IBuffer buffer = new byte[1024].AsBuffer();
                    IBuffer readTask = await BTSock.InputStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                    txtData.Text = UTF8Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.ToArray().Length);
                    System.Diagnostics.Debug.WriteLine(txtData.Text);
                    
                }
            }
        }



        // FUNCTION PROVIDED BY SPHERO
        private IBuffer GetBufferFromByteArray(byte[] package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteBytes(package);
                return dw.DetachBuffer();
            }
        }

        private async void lstBTPaired_Tap_1(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (lstBTPaired.SelectedItem == null) // To prevent errors, make sure something is Selected
                {
                    //btnConnectArduino.IsEnabled = false; // Make sure it's False if you want to use a Button
                    txtBTStatus.Text = "No Device Selected! Try again..."; // Set UI Output
                    return;
                }
                else
                    if (lstBTPaired.SelectedItem != null) // Just making sure something was Selected
                {

                    // btnConnectArduino.IsEnabled = true; // Since an item is Selected, Enable Connect Button (If using a Button)


                    /* This is a trick to Grab the Item and Remove '(' and ')' if using the Hostname & want just the Contents (00:00:00)
                    // Of course we don't HAVE to do this, but this is a C# Trick/Hack to learn String Functions
                    string ba = lstBTPaired.SelectedItem.ToString(); // Store the Tapped/Selected Item
                    int found = 0; // Set the Found to 0
                    found = ba.IndexOf("("); // Let's get the Index of the "(" in the String (ba)
                    ba = ba.Substring(found + 1); // Use Substring with the IndexOf
                    ba = ba.Replace(")", ""); // Now remove the last ")" in the String to be "00:00:00:00:00"
                    // Test our Hack by Uncommenting Below...
                    //MessageBox.Show(ba); - This is just to make sure we did it right */

                    PeerFinder.AlternateIdentities["Bluetooth:Paired"] = ""; // Grab Paired Devices
                    var PF = await PeerFinder.FindAllPeersAsync(); // Store Paired Devices

                    BTSock = new StreamSocket(); // Create a new Socket Connection
                    await BTSock.ConnectAsync(PF[lstBTPaired.SelectedIndex].HostName, "1"); // Connect using Socket to Selected Item

                    // Once Connected, let's give Arduino a HELLO
                    var datab = GetBufferFromByteArray(Encoding.UTF8.GetBytes("1")); // Create Buffer/Packet for Sending
                    await BTSock.OutputStream.WriteAsync(datab); // Send Arduino Buffer/Packet Message

                    btnSendCommand.IsEnabled = true; // Allow commands to be sent via Command Button (Enabled)
                    
                }
            }
            catch (Exception ex)
            {
                txtBTStatus.Text = "Faild to connect: retrying";
                lstBTPaired_Tap_1(sender, e);
                //txtData.Text = ex.StackTrace;
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.HResult);
               
            }
        }


        void PeerFinder_TriggeredConnectionStateChanged(object sender, TriggeredConnectionStateChangedEventArgs args)
        {
            // This will be used to Get Data from our Hardware soon

            if (args.State == TriggeredConnectState.Failed)
            {
                txtBTStatus.Text = "Failed to Connect... Try again!";
                BTStatus = "no"; // Not connected
                return;
            }

            if (args.State == TriggeredConnectState.Completed)
            {
                txtBTStatus.Text = "Connected!";
                BTStatus = "yes"; // Means we are connected
                BT2Arduino_Receive();
            }
        }

        private void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            // In this Demo, our Arduino code knows to look for "3" to turn off/on LED/Motor for 4 Seconds
            BT2Arduino_Send("2"); // This will send using the GoSend Feature
            BT2Arduino_Receive();

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

        }
    }
}
