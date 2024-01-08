using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
using OscManager;

namespace OmniModConnect
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public OscQueryServer? queryServer;	// These will never be null in practice, the ? is just there to make VS happy
		public OscClient? oscClient;
		public BluetoothManager? bluetooth;
		private InitView initView;
		private BluetoothListView btListView;
		private DeviceConnectedView? deviceConnectedView = null;


		public MainWindow()
		{
			InitializeComponent();
			initView = new InitView(this);
			btListView = new BluetoothListView(this);
			

			ShowInitView();
		}

		public void ShowInitView()
		{
			MainContent.Content = initView;
			initView.StartInit();
		}

		public void ShowBluetoothList()
		{
			MainContent.Content = btListView;
			deviceConnectedView = null;	// Remove the connection instance (used for disconnecting)
			btListView.ScanForBTDevices();
		}

		public void ShowDeviceConnectedView(BluetoothDevice device)
		{
            deviceConnectedView = new DeviceConnectedView(this, device);
			MainContent.Content = deviceConnectedView;
		}

		
	}
}