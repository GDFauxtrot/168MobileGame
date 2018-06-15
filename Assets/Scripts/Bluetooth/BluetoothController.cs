using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BluetoothController : MonoBehaviour, IBtObserver {

    public GameObject chatMessagePrefab;

    private Bluetooth bluetooth;

    public BluetoothModel bluetoothModel;

    public Dropdown deviceDropdown;

    public Button searchButton;

    public Button connectButton;

    public Text bluetoothConnected;

    public GameObject chatContent;

    public Scrollbar chatScrollbar;

    public InputField chatInputField;

    public Button chatSendButton;

    private TimeSpan handshakeTime;
    private DateTime startTime;

    private void Awake() {
        bluetooth = Bluetooth.GetInstance();
        startTime = System.DateTime.Now;
    }

    private void Start() {
        bluetoothModel.AddObserver(this);
        deviceDropdown.ClearOptions();
        deviceDropdown.AddOptions(new List<string>(new string[] { "none" }));
        connectButton.interactable = false;

        searchButton.onClick.AddListener(
            () => {
                bluetooth.SearchDevice();
                bluetoothModel.clearMacAddresses();
                connectButton.interactable = false;
                deviceDropdown.ClearOptions();
                deviceDropdown.AddOptions(new List<string>(new string[] { "..." }));
            });

        connectButton.onClick.AddListener(
             () => {
                 bluetooth.Connect(deviceDropdown.options[deviceDropdown.value].text);
             });

        chatSendButton.onClick.AddListener(
            () => {
                if (!chatInputField.text.Equals("") && bluetooth.IsConnected()) {
                    string message = bluetooth.DeviceName() + ": " + chatInputField.text;
                    // bluetoothConnected.text = bluetooth.Send(message);
                    bluetoothConnected.text = SendMessageProper(message);


                    GameObject chatMessage = Instantiate(chatMessagePrefab);
                    chatMessage.GetComponent<Text>().text = message;
                    chatMessage.transform.SetParent(chatContent.transform);
                    
                    chatInputField.text = "";
                }
            });
    }

    public string SendMessageProper(string message) {
        return bluetooth.Send(BluetoothModel.STARTCHAR + message + BluetoothModel.ENDCHAR);
    }
    public string StripMessage(string message) {
        if (message.StartsWith(BluetoothModel.STARTCHAR.ToString()) && message.EndsWith(BluetoothModel.ENDCHAR.ToString())) {
            message = message.Remove(0,1);
            message = message.Remove(message.Length-1,1);
        }
        return message;
    }

    public void OnStateChanged(string _State) {
        switch (int.Parse(_State)) {
            case 0:
                bluetoothConnected.text = "Not Connected";
                break;
            case 1:
                bluetoothConnected.text = "Connection Failed";
                break;
            case 2:
                bluetoothConnected.text = "Connecting...";
                break;
            case 3:
                bluetoothConnected.text = "Connected!";
                break;
            default:
                bluetoothConnected.text = "Unknown State: " + _State;
                break;
        }
        
        if (int.Parse(_State) == 3) {
            SendMessageProper("start:" + (DateTime.Now - startTime));
            handshakeTime = DateTime.Now - startTime;
        }
    }

    public void OnSendMessage(string _Message) {
        Debug.Log("Sending message: '" + _Message + "'");
    }

    public void OnGetMessage(string _Message) {
        Debug.Log("Received message: '" + _Message + "'");

        _Message = StripMessage(_Message);

        List<object> message = MessageParser.ParseMessage(_Message);

        string type = (string) message[0];

        if(type == "start")
        {
            TimeSpan theirTime = (TimeSpan) message[1];

            if(handshakeTime > theirTime)
            {
                GameManager.instance.playerType = PlayerType.Runner;
            }
            else if (handshakeTime < theirTime)
            {
                GameManager.instance.playerType = PlayerType.Blocker;
            } else {
                Application.Quit(); // fuck it, there's no way in hell
            }

            SceneManager.LoadScene(1);
        }
    }

    public void OnFoundNoDevice() {
        deviceDropdown.ClearOptions();
        deviceDropdown.AddOptions(new List<string>(new string[] { "none" }));
    }

    public void OnScanFinish() {
    }

    public void OnFoundDevice() {
        // Clear and Get new List
        connectButton.interactable = true;
        deviceDropdown.ClearOptions();
        deviceDropdown.AddOptions(bluetoothModel.macAddresses);
    }
}
