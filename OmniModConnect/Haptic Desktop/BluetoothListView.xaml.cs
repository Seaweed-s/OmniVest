using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using InTheHand.Bluetooth;

namespace OmniModConnect
{
	/// <summary>
	/// Interaction logic for BluetoothListView.xaml
	/// </summary>

	public partial class BluetoothListView : UserControl
	{
		public BluetoothDevice? currentDevice = null;

		//TODO: Abstract this a bit more to a settings menu or something
		private Guid OmnimodServiceID = new Guid("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
		private MainWindow mainWindow;
		private bool currentlyScanning = false;
		private ObservableCollection<BluetoothDeviceWrapper> bluetoothDevices;

		public BluetoothListView(MainWindow parentWindow)
		{
			InitializeComponent();

			mainWindow = parentWindow;

			bluetoothDevices = new ObservableCollection<BluetoothDeviceWrapper>();
			BTListBox.ItemsSource = bluetoothDevices;
		}

		public async void ScanForBTDevices()
		{
			if (currentlyScanning) return;

			DeviceNameText.Content = "Device Name:";
			DeviceIDText.Content = "Device ID:";
			OmnimodServiceText.Content = "Omnimod Service:";
			ConnectButton.IsEnabled = false;

			Debug.WriteLine("Starting Scanning for BT devices");
			ScanButton.Content = "Scanning...";

			currentlyScanning = true;
			ScanButton.IsEnabled = false;

			bluetoothDevices.Clear();

			var deviceList = await Bluetooth.ScanForDevicesAsync();

			foreach (var device in deviceList)
			{
				bluetoothDevices.Add(new BluetoothDeviceWrapper(device));
			}

			Debug.WriteLine("Scan Complete");
			ScanButton.Content = "Scan For Devices";

			currentlyScanning = false;
			ScanButton.IsEnabled = true;
		}

		private void BTListBox_SelChanged(object sender, SelectionChangedEventArgs e)
		{
			if (BTListBox.SelectedItem != null && BTListBox.SelectedItem is BluetoothDeviceWrapper selectedDevice)
			{
				Debug.WriteLine($"Device {BTListBox.SelectedItem} Selected");

				currentDevice = selectedDevice.Device;

				DisplayCurrentDevice();
			}
		}

		private async void DisplayCurrentDevice()
		{
			if(currentDevice == null) return;

			DeviceNameText.Content = $"Device Name: {currentDevice.Name}";
			DeviceIDText.Content = $"Device ID: {currentDevice.Id}";
			OmnimodServiceText.Content = "Omnimod Service: Connecting...";

			//RemoteGattServer gattServer = new RemoteGattServer(currentDevice);
			RemoteGattServer gattServer = currentDevice.Gatt;
			await gattServer.ConnectAsync();

			//TODO: Check for Omnimod Service
			GattService OmnimodService = await gattServer.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(OmnimodServiceID));

			if (OmnimodService == null)
			{
				OmnimodServiceText.Content = "Omnimod Service: Not found";
				ConnectButton.IsEnabled = false;
			} else
			{
				OmnimodServiceText.Content = "Omnimod Service: Found";
				ConnectButton.IsEnabled = true;
			}

			// TODO: Manage disconnecting from the device properly after scan is complete
			//gattServer.Disconnect();	// Disconnecting here causes issues (might be a problem with library?)
			// Seems to be a problem with Windows API. Solution may be connecting to the device directly
			// instead of using BluetoothDevice object returned in the array by the scan function?
			// https://github.com/inthehand/32feet/issues/300
			// Just not going to disconnect for now, it will cause issues though unfortunately...
		}

		private void ScanButton_Click(object sender, RoutedEventArgs e)
		{
			ScanForBTDevices();
		}

		private void ConnectButton_Click(Object sender, RoutedEventArgs e)
		{
			if (currentDevice == null) return;
			ConnectButton.IsEnabled = false;
			mainWindow.ShowDeviceConnectedView(currentDevice);
		}
	}

	internal class BluetoothDeviceWrapper
	{
		public BluetoothDevice Device { get; }

		public BluetoothDeviceWrapper(BluetoothDevice device)
		{
			Device = device;
		}

		public override string ToString()
		{
			if (Device.Name != "")
			{
				return Device.Name;
			}
			return Device.Id;
			
		}
	}
}
