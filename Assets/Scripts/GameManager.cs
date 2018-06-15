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
        if (btModel != null) {
            if (next == SceneManager.GetSceneByBuildIndex(1)) { // elegant af
                blockManager.generateGroundAheadOfPlayer = true;
            }
            if (!btModel.IsInObserverList(this)) {
                btModel.AddObserver(this);
            }
        } else {
            
        }
        
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
        
        if(type == "jump")
        {
            bool jumping = (bool) m[1];
            Vector3 pos = new Vector3((float)m[2], (float)m[3], (float)m[4]);
            if (jumping)
                playerController.RunnerDoJump(pos);
            else
                playerController.RunnerStopJump(pos);
        }
        if (type == "block") {
            Vector3 pos = new Vector3((float)m[1], (float)m[2], (float)m[3]);
            GameObject block = blockManager.GetFromPool();
            block.transform.position = pos;
            block.GetComponent<Block>().bitmask = -1; // Force an update in AdjustBitmasks
            blockManager.AdjustBitmasks(block);
        }
        if (type == "pos") {
            // Update position, incl. latency compensation
            System.DateTime theirDT = (System.DateTime) m[1];
            System.DateTime ourDT = System.DateTime.Now;
            System.TimeSpan difference = ourDT - theirDT;

            float secs = difference.Seconds + (difference.Milliseconds/1000f);

            Vector3 pos = new Vector3((float)m[2], (float)m[3], (float)m[4]);
            
            playerController.velocity.y += playerController.acceleration.y*(secs/Time.fixedDeltaTime);
            playerController.velocity.y = Mathf.Clamp(playerController.velocity.y, -PlayerController.TERMINAL_VELOCITY, PlayerController.TERMINAL_VELOCITY);

            playerController.transform.position = pos + new Vector3(playerController.velocity.x*(secs/Time.fixedDeltaTime), playerController.velocity.y*(secs/Time.fixedDeltaTime), 0);
        }
        if (type == "ded") {
            // TODO kill state
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

    // Message senders for ez pz code
    public void SendPlayerJump(bool jumping, Vector3 pos)
    {
        SendMessageProper("jump:"+jumping.ToString()+":"+pos.x+","+pos.y+","+pos.z);
    }
    public void CreateBlock(Vector3 pos) {
        SendMessageProper("block:"+pos.x+","+pos.y+","+pos.z);
    }
    public void SendPosition(Vector3 pos) {
        SendMessageProper("pos:"+System.DateTime.Now+":"+pos.x+","+pos.y+","+pos.z);
    }
    public void SendPlayerDead() {
        SendMessageProper("ded");
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
