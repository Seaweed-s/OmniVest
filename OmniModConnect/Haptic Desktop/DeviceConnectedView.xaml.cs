using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
using LucHeart.CoreOSC;

namespace OmniModConnect
{
	/// <summary>
	/// Interaction logic for DeviceConnectedView.xaml
	/// </summary>
	public partial class DeviceConnectedView : UserControl
	{
		//TODO: Abstract this a bit more to a settings menu or something
		private readonly Guid OmnimodServiceID = new Guid("4fafc201-1fb5-459e-8fcc-c5c9c331914b");	// UUID of Omnivest service
		private readonly Guid OmnimodMotorsID = new Guid("beb5483e-36e1-4688-b7f5-ea07361b26a8");	// UUID of Omnivest motors
		private const string omniModMotorsAddress = "/avatar/parameters/OmniMod/HapticContacts/";	// Address of contacts
		private const int motorCount = 84;	// There shouldn't be any more than 80 motors ever.

		private BluetoothDevice device;
		private RemoteGattServer? deviceGatt;
		private GattService? OmniService;
		private GattCharacteristic? OmniMotors;
		private MainWindow mainWindow;
		private ObservableCollection<MotorValue> motorValues = new ObservableCollection<MotorValue>();
		// Token to cancel the delay for the bluetooth sender loop, used for instant update.
		private CancellationTokenSource? btDelayCancel;
		private bool motorsUpdated = true;


		public DeviceConnectedView(MainWindow mainWindow, BluetoothDevice device)
		{
			InitializeComponent();
			this.device = device;
			this.mainWindow = mainWindow;

			for (int i = 0; i < motorCount; i++)
			{
				motorValues.Add(new MotorValue((byte)(i + 1), 0));
			}
			MotorsAmplitudes.ItemsSource = motorValues;

			ConnectToDevice();

			if (mainWindow.oscClient != null)
			{   // This should never be null in practice.
				mainWindow.oscClient.StartReciever();
				mainWindow.oscClient.MessageReceived += OscMessageReceived;
			}

		}

		private async Task SendMotorStates()
		{
			while (true)
			{
				if (OmniMotors != null)
				{
					if (!motorsUpdated)	// Motors array hasn't been updated so wait for update or keepalive timeout
					{
						//Debug.WriteLine("Waiting for update or keepalive");
						btDelayCancel = new CancellationTokenSource();
						Task delayTask = Task.Delay(1000, btDelayCancel.Token);
						Task continuationTask = delayTask.ContinueWith(task => { });	// This is needed to prevent TaskCancellation exceptions
                        await continuationTask;
                    }

					motorsUpdated = false;
					//Debug.WriteLine("Writing array over BT");
					await OmniMotors.WriteValueWithoutResponseAsync(motorValues.Select(item => item.Value).ToArray());
				}
			}
		}

		private void OscMessageReceived(object? sender, OscMessage message)
		{
			if(message.Address.StartsWith(omniModMotorsAddress))
			{
				//Debug.WriteLine("Haptic motor update");
				int lastIndex = message.Address.LastIndexOf('/');
				string lastPart = message.Address.Substring(lastIndex + 1);
				if (int.TryParse(lastPart, out int index) && index >= 0 && index < motorCount)
				{
					var argument = message.Arguments[0];
					string? value;
					if (argument != null)
					{
						value = argument.ToString();
					} else
					{
						return;	// No data was included in the packet
					}

					//Debug.WriteLine($"Update value = {value}");
					if (double.TryParse(value, out double result) && result >= 0 && result <= 1)
					{
						//Debug.WriteLine("Value valid, updating array");
						motorValues[index].Value = (byte)(result * 255);
						motorsUpdated = true;
						if (btDelayCancel != null)
						{
                            btDelayCancel.Cancel(); // Cancel keepalive delay and update immediately
                        }
					}
				}
			}
		}

		private async void ConnectToDevice()
		{
			deviceGatt = device.Gatt;
			await deviceGatt.ConnectAsync();

			OmniService = await deviceGatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(OmnimodServiceID));
			if (OmniService == null)
			{
				Debug.WriteLine("Omnivest service could not be found");
				Disconnect();
				return;
			}
			OmniMotors = await OmniService.GetCharacteristicAsync(BluetoothUuid.FromGuid(OmnimodMotorsID));

			Debug.WriteLine($"Connected to device with {deviceGatt.Mtu} MTU");


			_ = Task.Run(SendMotorStates);
		}

		private void DisconnectButton_Click(object sender, RoutedEventArgs e)
		{
			Disconnect();	
		}

		private void Disconnect()
		{
            if (mainWindow.oscClient != null)
            {
                mainWindow.oscClient.MessageReceived -= OscMessageReceived;
                mainWindow.oscClient.StopReciever();
            }
            else
            {   // This should never happen, if it does, something has gone VERY wrong.
                Debug.WriteLine("Could not stop OSC reciever, client is null");
            }
            if (deviceGatt != null)
            {
                deviceGatt.Disconnect();
            }
            mainWindow.ShowBluetoothList();
        }

		internal class MotorValue : INotifyPropertyChanged
		{
			public byte Index { get; }

			private byte value;
			public byte Value
			{
				get { return this.value; }
				set
				{
					if (this.value != value)
					{
						this.value = value;
						OnPropertyChanged(nameof(Value));
					}
				}
			}

			public MotorValue(byte  index, byte value)
			{
				Index = index;
				Value = value;
			}

			public event PropertyChangedEventHandler? PropertyChanged;

			protected virtual void OnPropertyChanged(string propertyName)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
