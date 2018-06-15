using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// https://gamedev.stackexchange.com/questions/130180/smooth-loading-screen-between-scenes
// This was insightful, just leaving it here

public enum PlayerType { Runner, Blocker };

public class GameManager : MonoBehaviour, IBtObserver {

    public static GameManager instance;

    // Major game components can be accessed from here
    Bluetooth bt;
    BluetoothModel btModel;
    BlockManager blockManager;
    PlayerController playerController;
    BoyoController boyoController;

    public PlayerType playerType;

    bool btAddedObserver;
    void Awake() {
        // This is a SingleTON of stuff
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        SceneManager.activeSceneChanged += ActiveSceneChanged;
        bt = Bluetooth.GetInstance();
    }

    public void ActiveSceneChanged(Scene current, Scene next) {
        if (next == SceneManager.GetSceneByBuildIndex(1)) { // elegant af
            blockManager.generateGroundAheadOfPlayer = true;
        }
    }

    void Start() {
        if (!btAddedObserver) {
            btModel.AddObserver(this);
            btAddedObserver = true;
        }
        
        //blockManager.generateGroundAheadOfPlayer = true;
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
    public void SetBoyoController(BoyoController bc) {
        boyoController = bc;
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
    public BoyoController GetBoyoController() {
        return boyoController;
    }

    // Interfaces we care about
    public void OnGetMessage(string _Message) {
        _Message = StripMessage(_Message);
        // other player is jumping, need to maybe set it so that the playercontroller has a bool looking at this
        List<object> m = MessageParser.ParseMessage(_Message);

        string type = (string) m[0];
        
        Debug.LogWarning(type);
        if(type == "j")
        {
            bool jumping = (bool) m[1];
            Debug.LogWarning(jumping.ToString());
            if (jumping)
                playerController.RunnerDoJump();
            //playerController.jumping = jumping;
        }
    
    
    }

    // Interfaces we don't care about
    public void OnSendMessage(string _Message) {
    }

    public void OnStateChanged(string _State) {
    }

    public void OnFoundNoDevice() {
    }

    public void OnScanFinish() {
    }

    public void OnFoundDevice() {
    }


    public void SendPlayerJump(bool jumping)
    {
        SendMessageProper("j:"+jumping.ToString());
    }

    public string SendMessageProper(string message) {
        return bt.Send(BluetoothModel.STARTCHAR + message + BluetoothModel.ENDCHAR);
    }
    public string StripMessage(string message) {
        if (message.StartsWith(BluetoothModel.STARTCHAR.ToString()) && message.EndsWith(BluetoothModel.ENDCHAR.ToString())) {
            message = message.Remove(0,1);
            message = message.Remove(message.Length-1,1);
        }
        return message;
    }
}
