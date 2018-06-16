using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoyoController : MonoBehaviour {

    public GameObject controlsParent;

    public Button obstacleButton, pitButton;
    public Image obstacleGreen, pitGreen;
    
    public float obstacleCooldown, pitCooldown;
    float obstacleCurrentCooldown, pitCurrentCooldown;

    float pitWidth, obstacleWidth;

    void Awake() {
        GameManager.instance.SetBoyoController(this);
        pitWidth = pitGreen.GetComponent<RectTransform>().sizeDelta.x;
        obstacleWidth = obstacleGreen.GetComponent<RectTransform>().sizeDelta.x;
        obstacleCurrentCooldown = obstacleCooldown;
        pitCurrentCooldown = pitCooldown;
    }

    void Start() {
        controlsParent.SetActive(GameManager.instance.playerType == PlayerType.Blocker);
    }

    void Update() {
        //if (GameManager.instance.playerType == PlayerType.Blocker) {
        //    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
        //        BlockManager bm = GameManager.instance.GetBlockManager();
        //        Vector3 mousePos = Input.mousePosition;
        //        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //        mousePos.z = 0;
        //        mousePos = new Vector3(Mathf.Floor(mousePos.x), Mathf.Floor(mousePos.y), mousePos.z);
        //        if (!bm.GetBlockInPos(mousePos)) {
        //            GameManager.instance.CreateBlock(mousePos);
        //            GameObject block = bm.GetFromPool();
        //            block.transform.position = mousePos;
        //            block.GetComponent<Block>().bitmask = -1; // Force an update in AdjustBitmasks
        //            bm.AdjustBitmasks(block);
        //        }
        //    }
        //}

        if (obstacleCurrentCooldown < obstacleCooldown) {
            obstacleCurrentCooldown += Time.deltaTime;
        } else {
            obstacleCurrentCooldown = obstacleCooldown;
            obstacleButton.interactable = true;
        }
        if (pitCurrentCooldown < pitCooldown) {
            pitCurrentCooldown += Time.deltaTime;
        } else {
            pitCurrentCooldown = pitCooldown;
            pitButton.interactable = true;
        }

        obstacleGreen.GetComponent<RectTransform>().sizeDelta = new Vector2(obstacleWidth * (obstacleCurrentCooldown / obstacleCooldown), obstacleGreen.GetComponent<RectTransform>().sizeDelta.y);
        pitGreen.GetComponent<RectTransform>().sizeDelta = new Vector2(pitWidth * (pitCurrentCooldown / pitCooldown), pitGreen.GetComponent<RectTransform>().sizeDelta.y);
    }

    public void ObstaclePressed() {
        obstacleButton.interactable = false;
        obstacleCurrentCooldown = 0;

        GameManager.instance.GetBlockManager().CreateRandomObstacle();
    }

    public void PitPressed() {
        pitButton.interactable = false;
        pitCurrentCooldown = 0;
        GameManager.instance.GetBlockManager().CreatePit();
    }

}
