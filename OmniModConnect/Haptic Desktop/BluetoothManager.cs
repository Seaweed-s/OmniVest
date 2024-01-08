using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using InTheHand.Bluetooth;
using System.Xaml;

namespace OmniModConnect
{
	public class BluetoothManager
	{
		//private const string vestName = "OmnivestESP";

		public BluetoothManager()
		{
			//_ = ScanDevices();
		}

		public async Task<bool> BluetoothAvailability()
		{
			var result = await Bluetooth.GetAvailabilityAsync();

			return result;
		}

		private async Task ScanDevices()
		{
			try
			{
				Debug.WriteLine("Scanning for devices");

				var devices = await Bluetooth.ScanForDevicesAsync();

				Debug.WriteLine($"Devices found: {devices.Count}");

				foreach (BluetoothDevice device in devices)
				{
					Debug.WriteLine(device.Name);
				}
			}catch (Exception ex)
			{
				Debug.WriteLine("Device Scan Failed With Exception:");
				Debug.WriteLine(ex.Message);
			}
			


		}
		
	}
}
