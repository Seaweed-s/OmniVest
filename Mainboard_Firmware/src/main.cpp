#include <Adafruit_NeoPixel.h>
#include <USBCDC.h>

#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>

#define NEOPIXEL_PIN 48
#define NEOPIXEL_COUNT 1

#define ADVERT_NAME "OmnivestESP"
#define SERVICE_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define HAPTIC_MOTORS_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define LED_COLOUR_UUID "8a0a472f-74c6-443d-96c3-5c17cfb20eaa"
#define CHARACTERISTIC_SIZE 84

// Create a NeoPixel object
Adafruit_NeoPixel neopixel = Adafruit_NeoPixel(NEOPIXEL_COUNT, NEOPIXEL_PIN, NEO_GRB + NEO_KHZ800);

BLEServer* bluetoothServer;
BLEAdvertising* bluetoothAdvert;

BLEService* mainVestService;
BLECharacteristic* hapticMotorsChar;
uint8_t motorAmplitudes[CHARACTERISTIC_SIZE];
BLECharacteristic* ledColourChar;
uint8_t ledRGB[3];

bool connected = false;

class MyServerCallbacks : public BLEServerCallbacks {
	void onConnect(BLEServer *pServer) {
		Serial.println("Device connected");
		connected = true;

		esp_ble_conn_update_params_t conn_params;
    	conn_params.min_int = 0x06; // x 1.25ms
    	conn_params.max_int = 0x20; // x 1.25ms
    	conn_params.latency = 0x00; //number of skippable connection events
    	conn_params.timeout = 0xA0; // x 6.25ms, time before peripheral will assume connection is dropped.
		esp_ble_gap_update_conn_params(&conn_params);

		Serial.println("Stopping advertising");
		bluetoothAdvert->stop(); // stop advertising after connection
	}

	void onDisconnect(BLEServer *pServer) {
		Serial.println("Device disconnected");
		connected = false;
		Serial.println("Restarting advertising");
		bluetoothAdvert->start(); // restart advertising after disconnecting

		for (int i = 0; i < 3; i++) {
		ledRGB[i] = 0;
		if (i == 1) {
			ledRGB[i] = 25;
		}
	}
	}
};

class HapticMotorCallback : public BLECharacteristicCallbacks {
  void onWrite(BLECharacteristic *pCharacteristic) {
	std::string newValue = pCharacteristic->getValue();
	// Serial.println("Recieved new motor data");
	if (newValue.length() <= CHARACTERISTIC_SIZE) {
		// for (int i = 0; i < newValue.length(); i++){
		// 	Serial.print(newValue[i]);
		// 	Serial.print(", ");
		// }
		// Serial.println("");
		Serial.println("--------------");

		for (int i = 0; i < newValue.length(); i++) {
			motorAmplitudes[i] = (uint8_t)newValue[i];
			Serial.print(motorAmplitudes[i]);
			Serial.print(", ");
		}
		Serial.println("");
		hapticMotorsChar->setValue(motorAmplitudes, sizeof(motorAmplitudes));
		// Serial.println("Updated Motor Array");
	} else {
		Serial.println("Provided data invalid, array was not updated.");\
	}
  }
};

class LEDColourCallback : public BLECharacteristicCallbacks {
	void onWrite(BLECharacteristic* pCharacteristic) {
		std::string newValue = pCharacteristic->getValue();

		if (newValue.length() == 3) {
			Serial.println("New LED data recieved");
			for (int i = 0; i < 3; i++) {
				ledRGB[i] = newValue[i];
				Serial.print(newValue[i]);
				Serial.print(", ");
			}
			Serial.println();
			ledColourChar->setValue(ledRGB, sizeof(ledRGB));
			Serial.println("Updated LED Array");
		}
	}
};

void initBluetooth() {
	BLEDevice::init(ADVERT_NAME);

	bluetoothServer = BLEDevice::createServer();
	bluetoothServer->setCallbacks(new MyServerCallbacks());

	mainVestService = bluetoothServer->createService(SERVICE_UUID);

	hapticMotorsChar = mainVestService->createCharacteristic(
		HAPTIC_MOTORS_UUID,
		BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_WRITE);
	hapticMotorsChar->setCallbacks(new HapticMotorCallback());
	BLEDescriptor motorDescriptor("78acdfcd-5852-4b1f-ba1b-cedd6710b1ed");
	motorDescriptor.setValue("Motor Amplitudes");
	hapticMotorsChar->addDescriptor(&motorDescriptor);

	ledColourChar = mainVestService->createCharacteristic(
		LED_COLOUR_UUID,
		BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_WRITE);
	ledColourChar->setCallbacks(new LEDColourCallback());
	
	//mainVestService->addCharacteristic(hapticMotorsChar);
	mainVestService->start();

	// Set the initial value of the characteristic
	for (int i = 0; i < CHARACTERISTIC_SIZE; i++) {
		motorAmplitudes[i] = 0;
	}
	hapticMotorsChar->setValue(motorAmplitudes, sizeof(motorAmplitudes));

	for (int i = 0; i < 3; i++) {
		ledRGB[i] = 0;
		if (i == 1) {
			ledRGB[i] = 25;
		}
	}
	ledColourChar->setValue(ledRGB, sizeof(ledRGB));
	

	bluetoothAdvert = BLEDevice::getAdvertising();
	bluetoothAdvert->addServiceUUID(mainVestService->getUUID());
	bluetoothAdvert->start();

}

void setup() {
	Serial.begin(115200);
	// Initialize Neopixel
	neopixel.begin();
	neopixel.show(); // Initialize all pixels to 'off'

	initBluetooth();
}

void loop() {
	// Turn on Neopixel
	if (!connected) {
		neopixel.setPixelColor(0, neopixel.Color(25, 0, 0));  // Red color
	} else {
		neopixel.setPixelColor(0, neopixel.Color(ledRGB[0], ledRGB[1], ledRGB[2]));  // Green color
	}
	
	neopixel.show();
	//Serial.println("Test");
	delay(500);  // Wait for half a second

	// Turn off Neopixel
	neopixel.setPixelColor(0, neopixel.Color(0, 0, 0));  // Turn off
	neopixel.show();
	delay(500);  // Wait for half a second


}