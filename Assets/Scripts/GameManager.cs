using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    public int score;
    public bool gameIsPlaying = true;

    GameObject scoreText;
    GameObject roleText;

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
                gameIsPlaying = true;
                scoreText = GameObject.Find("ScoreText");
                roleText = GameObject.Find("RoleText");
            }
            if (!btModel.IsInObserverList(this)) {
                btModel.AddObserver(this);
            }
        } else {
            
        }
        
    }

    void Update() {
        if (gameIsPlaying) {
            score = Mathf.FloorToInt(Time.timeSinceLevelLoad*4);
            scoreText.GetComponent<Text>().text = "Score: " + score.ToString();
            roleText.GetComponent<Text>().text = "You are the: " + playerType.ToString();
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

    public void PlayerDied() {
        gameIsPlaying = false;
        SendPlayerDead(score);
        blockManager.generateGroundAheadOfPlayer = false;
        playerController.gameObject.SetActive(false);
        scoreText.GetComponent<Text>().text = score.ToString();
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
            Debug.Log("LATENCY: " + secs);
            Vector3 pos = new Vector3((float)m[2], (float)m[3], (float)m[4]);
            Vector3 vel = new Vector3((float)m[5], (float)m[6]);

            playerController.velocity = vel;
            playerController.velocity.y += playerController.acceleration.y*secs;
            playerController.velocity.y = Mathf.Clamp(playerController.velocity.y, -PlayerController.TERMINAL_VELOCITY, PlayerController.TERMINAL_VELOCITY);

            playerController.transform.position = new Vector3(pos.x + playerController.velocity.x*secs, pos.y + playerController.velocity.y*secs, playerController.transform.position.z);

            blockManager.GenerateGround();
        }
        if (type == "ded") {
            gameIsPlaying = false;
            score = (int) m[1];
            blockManager.generateGroundAheadOfPlayer = false;
            playerController.gameObject.SetActive(false);
            scoreText.GetComponent<Text>().text = "Score: " + score.ToString();
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
        SendMessageProper("jump" + Bluetooth.MSG_SEP
            + jumping.ToString() + Bluetooth.MSG_SEP
            + pos.x + "," + pos.y + "," + pos.z);
    }
    public void CreateBlock(Vector3 pos) {
        SendMessageProper("block" + Bluetooth.MSG_SEP
            + pos.x + "," + pos.y + "," + pos.z);
    }
    public void SendPosition(Vector3 pos, Vector2 velocity) {
        SendMessageProper("pos" + Bluetooth.MSG_SEP
            + System.DateTime.Now + Bluetooth.MSG_SEP
            + pos.x + "," + pos.y + "," + pos.z + Bluetooth.MSG_SEP
            + velocity.x + "," + velocity.y);
    }
    public void SendPlayerDead(int score) {
        SendMessageProper("ded" + Bluetooth.MSG_SEP + score);
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
