using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public interface IBtObserver {
    void OnStateChanged(string _State);
    void OnSendMessage(string _Message);
    void OnGetMessage(string _Message);
    void OnFoundNoDevice();
    void OnScanFinish();
    void OnFoundDevice();
}

public abstract class BtObservable : MonoBehaviour {
    protected List<IBtObserver> observerList;
    public abstract void AddObserver(IBtObserver _btObserver);
    public abstract void RemoveObserver(IBtObserver _btObserver);
}

public class BluetoothModel : BtObservable {

    [SerializeField]
    private int bufferSize = 256;
    [SerializeField]
    public const char STARTCHAR = '$';
    [SerializeField]
    public const char ENDCHAR = '#';

    public List<string> macAddresses = null;
    private Queue<string> messageQueue = null;
    private StringBuilder rawMessage = null;

    void Awake() {
        observerList = new List<IBtObserver>();

        macAddresses = new List<string>();
        messageQueue = new Queue<string>();
        rawMessage = new StringBuilder(bufferSize);
    }

    public void clearMacAddresses() {
        macAddresses.Clear();
    }

    private void CheckMessageFormat() {
        int startPos = -1;
        int endPos = -1;
        for(int i = 0; i < rawMessage.Length; ++i) {
            if(startPos == -1 && rawMessage[i] == STARTCHAR) { // Just look for first occurrence
                startPos = i;
            }
            else if(rawMessage[i] == ENDCHAR) { // Keep searching until last occurrence (message may contain this char)
                endPos = i;
            }
        }

        if(startPos != -1 && endPos != -1) {
            Debug.Log("Found message start and end points: " + startPos + ", " + endPos);
            messageQueue.Enqueue(rawMessage.ToString(startPos, endPos-startPos+1));
            rawMessage.Remove(startPos, endPos-startPos+1);
        }
        
        string tempMassege = messageQueue.Dequeue();

        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnGetMessage(tempMassege);
        }

        Debug.Log("Get Packet and Enqueue messageQueue");
        Debug.Log(rawMessage);
    }

    // ========================================
    //             Pattern Method
    // ========================================

    public override void AddObserver(IBtObserver _btObserver) {
        observerList.Add(_btObserver);
    }

    public override void RemoveObserver(IBtObserver _btObserver) {
        if (observerList.Contains(_btObserver)) {
            observerList.Remove(_btObserver);
        }
    }

    // ========================================
    //    Receive Bluetooth Call Back Method
    // ========================================

    void OnStateChanged(string _State) {
        //"STATE_CONNECTED"
        //"STATE_CONNECTING"
        //"UNABLE TO CONNECT"
        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnStateChanged(_State);
        }
        Debug.Log(_State);
    }

    void OnSendMessage(string _Message) {
        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnSendMessage(_Message);
        }
        Debug.Log("On Send Message : " + _Message);
    }

    void OnReadMessage(string _Message) {
        rawMessage.Append(_Message);
        CheckMessageFormat();
        Debug.Log("On Read Message : " + _Message);
    }

    void OnFoundNoDevice(string _s) {
        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnFoundNoDevice();
        }
        Debug.Log("On Found No Device");
    }

    void OnScanFinish(string _s) {
        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnScanFinish();
        }
        Debug.Log("On Scan Finish");
    }

    void OnFoundDevice(string _Device) {
        string device = _Device.Split(',')[0];
        if (device.Equals("null"))
            device = "???";
        macAddresses.Add(_Device);
        for (int i = 0; i < observerList.Count; ++i) {
            observerList[i].OnFoundDevice();
        }
        Debug.Log("On Found Device");
    }
}
