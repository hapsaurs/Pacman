using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public GameObject pacman;

    public GameObject leftWarpNode;
    public GameObject rightWarpNode;

    public AudioSource siren;
    public AudioSource munch1;
    public AudioSource munch2;
    public AudioSource powerPelletAudio;
    public AudioSource respawningAudio;
    public AudioSource ghostEatenAudio;

    public int currentMunch = 0;

    public int score;
    public TextMeshProUGUI scoreText;

    public GameObject ghostNodeLeft;
    public GameObject ghostNodeRight;
    public GameObject ghostNodeCenter;
    public GameObject ghostNodeStart;

    public GameObject redGhost;
    public GameObject pinkGhost;
    public GameObject blueGhost;
    public GameObject orangeGhost;

    public EnemyController redGhostController;
    public EnemyController pinkGhostController;
    public EnemyController blueGhostController;
    public EnemyController orangeGhostController;

    public int totalPellets;
    public int pelletsLeft;
    public int pelletsCollectedOnThisLife;

    public bool hadDeathOnThisLevel = false;

    public bool gameIsRunning;

    public List<NodeController> nodeControllers = new List<NodeController>();

    public bool newGame;
    public bool clearedLevel;

    public AudioSource startGameAudio;
    public AudioSource death;

    public int lives;
    public int currentLevel;

    public Image blackBackground;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI livesText;

    public bool isPowerPelletRunning = false;
    public float currentPowerPelletTime = 0;
    public float powerPelletTimer = 8f;
    public int powerPelletMultiPlyer = 1;

    public enum GhostMode
    {
        chase, scatter
    }

    public GhostMode currentGhostMode;

    public int[] ghostModeTimers = new int[] { 7, 20, 7, 20, 5, 20, 5 };
    public int ghostModeTimerIndex;
    public float ghostModeTimer = 0;
    public bool runningTimer;
    public bool completedTimer;



    // Start is called before the first frame update
    void Awake()
    {
        newGame = true;
        clearedLevel = false;
        blackBackground.enabled = false;

        // safe-get ghost controllers (Inspector must still assign GameObjects)
        if (redGhost != null) redGhostController = redGhost.GetComponent<EnemyController>();
        if (pinkGhost != null) pinkGhostController = pinkGhost.GetComponent<EnemyController>();
        if (blueGhost != null) blueGhostController = blueGhost.GetComponent<EnemyController>();
        if (orangeGhost != null) orangeGhostController = orangeGhost.GetComponent<EnemyController>();

        if (ghostNodeStart != null)
        {
            var node = ghostNodeStart.GetComponent<NodeController>();
            if (node != null) node.isGhostStartingNode = true;
        }

        // prefer assigning pacman in Inspector, else try to find by exact name
        if (pacman == null)
        {
            pacman = GameObject.Find("Player"); // case-sensitive
            if (pacman == null)
            {
                Debug.LogWarning("GameManager: pacman not assigned and GameObject 'Player' not found.");
            }
        }

        StartCoroutine(Setup());
    }

    public IEnumerator Setup()
    {
        ghostModeTimerIndex = 0;
        ghostModeTimer = 0;
        completedTimer = false;
        runningTimer = true;
        gameOverText.enabled = false;
        // ensure other objects' Awake() have run (avoid race where this coroutine runs before other Awakes)
        yield return null;

        if (clearedLevel)
        {
            blackBackground.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }
        blackBackground.enabled = false;

        pelletsCollectedOnThisLife = 0;
        currentGhostMode = GhostMode.scatter;
        gameIsRunning = false;

        float waitTimer = 1f;

        if (clearedLevel || newGame)
        {
            pelletsLeft = totalPellets;
            waitTimer = 4f;
            for (int i = 0; i < nodeControllers.Count; i++)
            {
                nodeControllers[i].RespawnPellet();
            }
        }

        if (newGame)
        {
            startGameAudio?.Play();
            score = 0;
            if (scoreText != null) scoreText.text = "Score: " + score.ToString();
            SetLives(3);
            currentLevel = 1;
        }

        // call PlayerController.Setup() only if pacman and component exist
        if (pacman != null)
        {
            var playerCtrl = pacman.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Setup();
            }
            else
            {
                Debug.LogError("GameManager: 'pacman' does not have PlayerController component.");
            }
        }
        else
        {
            Debug.LogError("GameManager: pacman is null. Assign pacman in Inspector or name the player object 'Player'.");
        }

        redGhostController?.Setup();
        pinkGhostController?.Setup();
        blueGhostController?.Setup();
        orangeGhostController?.Setup();

        newGame = false;
        clearedLevel = false;
        yield return new WaitForSeconds(waitTimer);

        StartGame();
    }

    void SetLives(int newLives)
    {
        lives = newLives;
        livesText.text = "Lives: " + lives;
    }

    void StartGame()
    {
        gameIsRunning = true;
        siren?.Play();
    }

    void stopGame()
    {
        gameIsRunning = false;
        siren.Stop();
        powerPelletAudio.Stop();
        respawningAudio.Stop();
        pacman.GetComponent<PlayerController>().Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameIsRunning)
        {
            return;

        }

        if (redGhostController.ghostNodeState == EnemyController.GhostNodeStatesEnum.respawning ||
            pinkGhostController.ghostNodeState == EnemyController.GhostNodeStatesEnum.respawning ||
            blueGhostController.ghostNodeState == EnemyController.GhostNodeStatesEnum.respawning ||
            orangeGhostController.ghostNodeState == EnemyController.GhostNodeStatesEnum.respawning)
        {
            if (!respawningAudio.isPlaying)
            {
                respawningAudio.Play();
            }
            else
            {
                if (respawningAudio.isPlaying)
                {
                    respawningAudio.Stop();
                }
            }
        }

        if (!completedTimer && runningTimer)
        {
            ghostModeTimer += Time.deltaTime;
            if (ghostModeTimer >= ghostModeTimers[ghostModeTimerIndex])
            {
                ghostModeTimer = 0;
                ghostModeTimerIndex++;

                if (currentGhostMode == GhostMode.chase)
                {
                    currentGhostMode = GhostMode.scatter;
                }
                else
                {
                    currentGhostMode = GhostMode.chase;
                }

                if (ghostModeTimerIndex == ghostModeTimers.Length)
                {
                    completedTimer = true;
                    runningTimer = false;
                    currentGhostMode = GhostMode.chase;
                }
            }

        }
        if (isPowerPelletRunning)
        {
            currentPowerPelletTime += Time.deltaTime;
            if (currentPowerPelletTime >= powerPelletTimer)
            {
                isPowerPelletRunning = false;
                currentPowerPelletTime = 0;
                powerPelletAudio.Stop();
                siren.Play();
                powerPelletMultiPlyer = 1;
            }
        }
    }

    public void GotPelletFromNodeController(NodeController nodeController)
    {
        nodeControllers.Add(nodeController);
        totalPellets++;
        pelletsLeft++;
    }

    public void AddToScore(int amount)
    {
        score += amount;
        if (scoreText != null) scoreText.text = "Score " + score.ToString();
    }
    public IEnumerator CollectedPellet(NodeController nodeController)
    {
        if (currentMunch == 0)
        {
            munch1?.Play();
            currentMunch = 1;
        }
        else if (currentMunch == 1)
        {
            munch2?.Play();
            currentMunch = 0;
        }
        pelletsLeft--;
        pelletsCollectedOnThisLife++;

        int requiredBluePellets = 0;
        int requiredOrangePellets = 0;

        if (hadDeathOnThisLevel)
        {
            requiredBluePellets = 12;
            requiredOrangePellets = 32;
        }
        else
        {
            requiredBluePellets = 30;
            requiredOrangePellets = 60;
        }

        if (pelletsCollectedOnThisLife >= requiredBluePellets && blueGhost != null)
        {
            var ec = blueGhost.GetComponent<EnemyController>();
            if (ec != null && !ec.leftHomeBefore) ec.readyToLeaveHome = true;
        }
        if (pelletsCollectedOnThisLife >= requiredOrangePellets && orangeGhost != null)
        {
            var ec = orangeGhost.GetComponent<EnemyController>();
            if (ec != null && !ec.leftHomeBefore) ec.readyToLeaveHome = true;
        }

        AddToScore(10);

        if (pelletsLeft == 0)
        {
            currentLevel++;
            clearedLevel = true;
            stopGame();
            yield return new WaitForSeconds(1);
            StartCoroutine(Setup());
        }
        if (nodeController.isPowerPellet)
        {
            siren.Stop();
            powerPelletAudio.Play();
            isPowerPelletRunning = true;
            currentPowerPelletTime = 0;
            

            redGhostController.SetFrightened(true);
            pinkGhostController.SetFrightened(true);
            blueGhostController.SetFrightened(true);
            orangeGhostController.SetFrightened(true);
        }
    }

    public IEnumerator PauseGame(float timeToPause)
    {
        gameIsRunning = false;
        yield return new WaitForSeconds(timeToPause);
        gameIsRunning = true;
    }
    public void GhostEaten()
    {
        ghostEatenAudio.Play();
        AddToScore(400 * powerPelletMultiPlyer);
        powerPelletMultiPlyer++;
        StartCoroutine(PauseGame(1));
    }

    public IEnumerator PlayerEaten() 
    { 
        hadDeathOnThisLevel = true;
        stopGame();
        yield return new WaitForSeconds(1);

        redGhostController.SetVisible(false);
        pinkGhostController.SetVisible(false);
        blueGhostController.SetVisible(false);
        orangeGhostController.SetVisible(false);

        pacman.GetComponent<PlayerController>().Death();
        death.Play();
        yield return new WaitForSeconds(3);
        SetLives(lives - 1);
        
        if (lives <= 0)
        {
            newGame = true;
            gameOverText.enabled = true;
            yield return new WaitForSeconds(3);
        }
        
        StartCoroutine(Setup());
    }
}