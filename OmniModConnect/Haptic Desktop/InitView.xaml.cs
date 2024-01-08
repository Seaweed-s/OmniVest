using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OscManager;

namespace OmniModConnect
{
	/// <summary>
	/// Interaction logic for InitView.xaml
	/// </summary>
	public partial class InitView : UserControl
	{
		private MainWindow mainWindow;

		public InitView(MainWindow parentWindow)
		{
			InitializeComponent();
			InitHeading.Content = "Application Initialising";
			InitContext.Text = "";

            mainWindow = parentWindow;
        }

		public async void StartInit()
		{
            mainWindow.queryServer = new OscQueryServer("OmniModConnect", "127.0.0.1");
            mainWindow.oscClient = new OscClient(9000, mainWindow.queryServer.udpPort);
            mainWindow.bluetooth = new BluetoothManager();

			if (await mainWindow.bluetooth.BluetoothAvailability())
			{
				mainWindow.ShowBluetoothList();
			} else
			{
				InitHeading.Content = "Initialisation Error";
				InitContext.Text = "Bluetooth could not be initialised." + "\n" +
					"Please ensure Bluetooth is enabled and your Bluetooth dongle is connected.";
			}
        }
	}
}
