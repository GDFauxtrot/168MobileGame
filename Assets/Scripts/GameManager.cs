using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// https://gamedev.stackexchange.com/questions/130180/smooth-loading-screen-between-scenes
// This was insightful, just leaving it here

public class GameManager : MonoBehaviour {

    public static GameManager instance;

    // Major game components can be accessed from here
    Bluetooth bt;
    BluetoothModel btModel;
    BlockManager blockManager;
    PlayerController playerController;
    //SpawnerController spawnerController;

    void Awake() {
        // This is a SingleTON of stuff
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        bt = Bluetooth.GetInstance();
    }

    void Start() {
        blockManager.generateGroundAheadOfPlayer = true;
    }

    // Trying a different approach for GM systems that require being a GameObject component eliminating .Find() - 
    // let them contact US on Start (the rest of the systems are regular Objects instantiated in Awake)
    public void SetBluetoothModel(BluetoothModel bm) {
        btModel = bm;
    }
    public void SetBlockManager(BlockManager bm) {
        blockManager = bm;
    }
    public void SetPlayerController(PlayerController pc) {
        playerController = pc;
    }

    public BluetoothModel GetBluetoothModel() {
        return btModel;
    }
    public BlockManager GetBlockManager() {
        return blockManager;
    }
    public PlayerController GetPlayerController() {
        return playerController;
    }
}
