using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Java-side implementation source code
// https://www.programcreek.com/java-api-examples/?code=rlatkdgus500/UnityBluetoothPlugin/UnityBluetoothPlugin-master/Assets/Scripts/Bluetooth/AndroidJavaFile/BluetoothPlugin.java

// This was a big help too
// https://labs.karmaninteractive.com/creating-android-jar-and-aar-plugins-for-unity-c293bb5258c9

public class Bluetooth {

    private AndroidJavaClass plugin;
    private AndroidJavaObject activityObject;
    private static Bluetooth instance;

    // Use to determine if we're on Android or not (testing on PC good -- pushing APK to Android dozens of times bad)
    public static bool connectedToAndroid = true;

    private Bluetooth() {}
    public static Bluetooth GetInstance() {
        if(instance == null) {
            instance = new Bluetooth();
            try {
                instance.PluginStart();
                instance.Discoverable();
                connectedToAndroid = true;
            } catch (Exception) {
                Debug.LogWarning("WARNING: Android environment not found! If it's supposed to (i.e. not testing/debugging), something terrible happened!");
                connectedToAndroid = false;
            }
        }
        return instance;
    }

    // ========================================
    //          Call Android Method
    // ========================================

    private void PluginStart() {
        plugin = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        activityObject = plugin.GetStatic<AndroidJavaObject>("currentActivity");
        activityObject.Call("StartPlugin");
    }

    public string Send(string message) {
        return activityObject.Call<string>("SendMessage", message);
    }

    public string SearchDevice() {
       Debug.Log("unity -> android | SearchDevice");
       return activityObject.Call<string>("ScanDevice");       
    }

    public string GetDeviceConnectedName() {
       return activityObject.Call<string>("GetDeviceConnectedName");
    }

    public string Discoverable() {
        return activityObject.Call<string>("EnsureDiscoverable");
    }

    public void Connect(string address) {
        activityObject.Call("Connect", address);
    }

    public string EnableBluetooth() {
        return activityObject.Call<string>("BluetoothEnable");
    }

    public string DisableBluetooth() {
        return activityObject.Call<string>("DisableBluetooth");
    }

    public string DeviceName() {
        return activityObject.Call<string>("DeviceName");
    }

    public bool IsEnabled() {
        return activityObject.Call<bool>("IsEnabled");
    }

    public bool IsConnected() {
        return activityObject.Call<bool>("IsConnected");
    }

	public void Stop() {
		activityObject.Call("StopThread");
	}

	public void ShowMessage(string message) {
		activityObject.Call("ShowMessage", message);
	}
}