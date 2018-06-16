using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour {

    public const int BITMASK_NORTH = 1;
    public const int BITMASK_WEST  = 2;
    public const int BITMASK_EAST  = 4;
    public const int BITMASK_SOUTH = 8;

    public GameObject baseBlockPrefab;

    public const int POOL_MAX = 512;

    public string blockGroupName;
    public string blockExtrasGroupName;

    public float blockVisibleWidth; // Blocks spawned at +blockVisibleWidth, destroyed at -blockVisibleWidth
    public float groundYValue, groundYBottom;

    public bool generateGroundAheadOfPlayer;

    Sprite[] blockSprites;
    Sprite[] blockExtraSprites;

    Queue<GameObject> pool;
    List<GameObject> releasedObjects;

    void Awake() {
        pool = new Queue<GameObject>();
        releasedObjects = new List<GameObject>();

        blockSprites = Resources.LoadAll<Sprite>(blockGroupName);
        blockExtraSprites = Resources.LoadAll<Sprite>(blockExtrasGroupName);

        for (int i = 0; i < POOL_MAX; ++i) {
            GameObject child = Instantiate(baseBlockPrefab);
            child.transform.SetParent(transform);
            child.SetActive(false);
            pool.Enqueue(child);
        }

        // Tell the GM we exist (safe to do in Awake w/ modified script execution order)
        GameManager.instance.SetBlockManager(this);
    }

    void Start() {
        GenerateFullGround();
    }
    void Update() {
        if (generateGroundAheadOfPlayer) {
            GenerateGround();
            
        }
    }

    public void GenerateGround() {
        Vector3 playerPos = GameManager.instance.GetPlayerController().transform.position;
            Vector3 blockPos = new Vector3(Mathf.Floor(playerPos.x), groundYValue, playerPos.z);
            for (float y = blockPos.y; y > groundYBottom; --y) {
                for (float x = (blockPos.x + blockVisibleWidth)-1; x < (blockPos.x + blockVisibleWidth); ++x) {
                    Vector3 pos = new Vector3(x, y, blockPos.z);
                    if (!GetBlockInPos(pos)) {
                        GameObject block = GetFromPool();
                        block.transform.position = pos;
                        block.GetComponent<Block>().bitmask = -1; // Force an update in AdjustBitmasks
                        AdjustBitmasks(block);
                    }
                }
            }

            Collider2D[] toBeDestroyed = Physics2D.OverlapBoxAll(new Vector2(playerPos.x - (blockVisibleWidth + 5f), playerPos.y), new Vector2(1f, 512f), 0, 1 << LayerMask.NameToLayer("Block"));
            foreach (Collider2D col in toBeDestroyed) {
                PutBackInPool(col.gameObject);
                // Force adjustments to nearby blocks
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.up));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.left));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.right));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.down));
            }
    }
    public void GenerateFullGround() {
        Vector3 playerPos = GameManager.instance.GetPlayerController().transform.position;
            Vector3 blockPos = new Vector3(Mathf.Floor(playerPos.x), groundYValue, playerPos.z);
            for (float y = blockPos.y; y > groundYBottom; --y) {
                for (float x = (blockPos.x - blockVisibleWidth); x < (blockPos.x + blockVisibleWidth); ++x) {
                    Vector3 pos = new Vector3(x, y, blockPos.z);
                    if (!GetBlockInPos(pos)) {
                        GameObject block = GetFromPool();
                        block.transform.position = pos;
                        block.GetComponent<Block>().bitmask = -1; // Force an update in AdjustBitmasks
                        AdjustBitmasks(block);
                    }
                }
            }

            Collider2D[] toBeDestroyed = Physics2D.OverlapBoxAll(new Vector2(playerPos.x - (blockVisibleWidth + 5f), playerPos.y), new Vector2(1f, 512f), 0, 1 << LayerMask.NameToLayer("Block"));
            foreach (Collider2D col in toBeDestroyed) {
                PutBackInPool(col.gameObject);
                // Force adjustments to nearby blocks
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.up));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.left));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.right));
                AdjustBitmasks(GetBlockInPos(col.transform.position + Vector3.down));
            }
    }

    // Recursive bitmask adjustment for a placed block
    public void AdjustBitmasks(GameObject block) {
        if (block == null)
            return;

        int bitmask = GetBitmask(block.transform.position);

        if (bitmask != block.GetComponent<Block>().bitmask) {
            block.GetComponent<SpriteRenderer>().sprite = blockSprites[bitmask];
            block.GetComponent<Block>().bitmask = bitmask;
            
            // Extra embellishment for nearby blocks w/ grass ends, these are safe to instantiate & destroy in small amounts
            //if (bitmask == 4 || bitmask == 12 || bitmask == 8 || bitmask == 0) {
            //    GameObject b = Instantiate(baseBlockPrefab);
            //    b.transform.position = new Vector3(block.transform.position.x - 1, block.transform.position.y, block.transform.position.z);
            //    b.GetComponent<SpriteRenderer>().sprite = blockExtraSprites[0];
            //    b.GetComponent<BoxCollider2D>().isTrigger = true;
            //    b.transform.SetParent(block.transform);
            //}
            //if (bitmask == 2 || bitmask == 10 || bitmask == 8 || bitmask == 0) {
            //    GameObject b = Instantiate(baseBlockPrefab);
            //    b.transform.position = new Vector3(block.transform.position.x + 1, block.transform.position.y, block.transform.position.z);
            //    b.GetComponent<SpriteRenderer>().sprite = blockExtraSprites[1];
            //    b.GetComponent<BoxCollider2D>().isTrigger = true;
            //    b.transform.SetParent(block.transform);
            //}

            // Recursion yay
            AdjustBitmasks(GetBlockInPos(block.transform.position + Vector3.up));
            AdjustBitmasks(GetBlockInPos(block.transform.position + Vector3.left));
            AdjustBitmasks(GetBlockInPos(block.transform.position + Vector3.right));
            AdjustBitmasks(GetBlockInPos(block.transform.position + Vector3.down));
        }
    }

    // - The big two: Get from pool, add back into pool - //

    public GameObject GetFromPool() {
        if (pool.Count == 0) {
            Debug.LogError("Trying to get from an empty pool! Release some objects or raise the object cap!");
            return null;
        }
        GameObject obj = pool.Dequeue();
        releasedObjects.Add(obj);

        obj.SetActive(true);
        return obj;
    }

    public void PutBackInPool(GameObject obj) {
        if (pool.Contains(obj)) {
            Debug.LogError("Trying to put back '" + obj.name + "' which is already inside of this pool!");
            return;
        }
        if (!releasedObjects.Contains(obj)) {
            Debug.LogError("Trying to put back '" + obj.name + "' which is not associated with this pool!");
            return;
        }

        foreach (Transform child in obj.transform) {
            Destroy(child.gameObject);
        }

        releasedObjects.Remove(obj);
        pool.Enqueue(obj);

        obj.SetActive(false);
    }

    // - Assistance (bitmask methods, helper methods, etc.) - //

    public GameObject GetBlockInPos(Vector3 pos) {
        Collider2D c = Physics2D.OverlapPoint(pos, 1 << LayerMask.NameToLayer("Block"));
        return c ? c.gameObject : null;
    }

    int GetBitmask(Vector3 pos) {
        int mask = 0;

        Collider2D[] colsNorth = Physics2D.OverlapPointAll(new Vector2(pos.x, pos.y + 1), 1 << LayerMask.NameToLayer("Block"));
        if (colsNorth.Length > 0)
            mask += BITMASK_NORTH;
        
        Collider2D[] colsWest  = Physics2D.OverlapPointAll(new Vector2(pos.x - 1, pos.y), 1 << LayerMask.NameToLayer("Block"));
        if (colsWest.Length > 0)
            mask += BITMASK_WEST;
        
        Collider2D[] colsEast = Physics2D.OverlapPointAll(new Vector2(pos.x + 1, pos.y), 1 << LayerMask.NameToLayer("Block"));
        if (colsEast.Length > 0)
            mask += BITMASK_EAST;
        
        Collider2D[] colsSouth = Physics2D.OverlapPointAll(new Vector2(pos.x, pos.y - 1), 1 << LayerMask.NameToLayer("Block"));
         if (colsSouth.Length > 0)
            mask += BITMASK_SOUTH;

        return mask;
    }

    // - BoyoController methods - //

    public void CreateRandomObstacle() {
        Debug.LogWarning("CREATE RANDOM OBSTACLE");
        bool[][] obstacle1 = new bool[3][] {
            new bool[7] {false, false, false, false, false, false, true},
            new bool[7] {false, false, false, true, false, false, true},
            new bool[7] {true, false, false, true, false, false, true}
        };

        bool[][] obstacle2 = new bool[2][] {
            new bool[8] {false, false, true, true, true, true, true, true},
            new bool[8] {true, true, true, true, true, true, true, true}
        };

        bool[][] obstacle3 = new bool[5][] {
            new bool[9] {true, false, false, false, true, false, false, false, false },
            new bool[9] {true, false, false, false, false, false, false, false, false, },
            new bool[9] {false, false, false, false, false, false, false, false, true },
            new bool[9] {false, false, false, false, true, false, false, false, true },
            new bool[9] {true, false, false, false, true, true, true, false, true }
        };
        int pick = Random.Range(0, 3);

        bool[][] obstacle;
        
        if (pick == 0) {
            obstacle = obstacle1;
        } else if (pick == 1) {
            obstacle = obstacle2;
        } else {
            obstacle = obstacle3;
        }

        Debug.LogWarning("PICKED OBSTACLE " + pick);

        List<Vector3> blocks = new List<Vector3>();

        for(int y = 0; y < obstacle.Length; ++y) {
            for (int x = 0; x < obstacle[y].Length; ++x) {
                if (obstacle[(obstacle.Length-1)-y][x]) { // Read array backwards but iterate forwards for Y
                    GameObject block = GetFromPool();
                    Debug.LogWarning("CREATE BLOCK");
                    if (Bluetooth.connectedToAndroid) {
                        //GameManager.instance.CreateBlock(new Vector3(x, y, block.transform.position.z));
                        blocks.Add(new Vector3(Mathf.Floor(GameManager.instance.GetPlayerController().transform.position.x)+blockVisibleWidth+x, groundYValue+y+1, block.transform.position.z));
                    }
                        
                    block.transform.position = new Vector3(Mathf.Floor(GameManager.instance.GetPlayerController().transform.position.x)+blockVisibleWidth+x, groundYValue+y+1, block.transform.position.z);
                    block.GetComponent<Block>().bitmask = -1;
                    AdjustBitmasks(block);
                }
            }
        }

        Debug.LogWarning("PASSING BLOCKS TO PEER");
        GameManager.instance.CreateBlocks(blocks);
    }

    public void CreatePit() {
        int count = 3;
        StartCoroutine(DelayGroundSpawningForCount(count));
        GameManager.instance.SendPit(count);
    }

    public IEnumerator DelayGroundSpawningForCount(int count) {
        float xPos = Mathf.Floor(GameManager.instance.GetPlayerController().transform.position.x);
        generateGroundAheadOfPlayer = false;
        yield return new WaitUntil(() => {
            return Mathf.Floor(GameManager.instance.GetPlayerController().transform.position.x) - xPos > count;
            });
        generateGroundAheadOfPlayer = true;
    }
}
