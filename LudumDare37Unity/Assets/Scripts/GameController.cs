﻿using UnityEngine;
using MaxPostnikov.Utils;

namespace MaxPostnikov.LD37
{
    public interface IUIController
    {
        void Restart();
    }

    public class GameController : MonoBehaviour, IUIController
    {
        const int c_SpawnTriesThreshold = 100;

        [Header("Refs")]
        public Shell shell;
        public NpcBubble[] npcBubblePrefabs;

        [Header("UI Refs")]
        public ProgressBar progressBar;
        public GameOverPopup gameOverPopup;

        [Header("Settings")]
        public int npcOnStart = 10;
        public int npcOnRecycle = 2;
        public float minShellRadius = 1f;
        public int scorePerFriend = 50;

        [Header("Spawn Settings")]
        public Vector2 spawnRange = new Vector2(-5f, 5f);
        public float spawnRangeIncr = 1.1f;
        public float spawnNpcDist = 3f;
        public float spawnShellDist = 2f;

        int totalScore;
        bool isGameOver;
        PrefabsPool<NpcBubble> npcBubblePool;

        int TotalScore {
            get {
                return totalScore;
            }
            set {
                totalScore = value;

                progressBar.SetScore(value);
            }
        }

        void Start()
        {
            gameOverPopup.Init(this);

            shell.Init();
            shell.RadiusChange += OnShellRadiusChange;
            
            npcBubblePool = new PrefabsPool<NpcBubble>(npcBubblePrefabs, transform, 3);
            npcBubblePool.Recycled += OnNpcRecycled;

            Reset();
        }

        void Reset()
        {
            progressBar.Show();
            
            shell.Reset();

            npcBubblePool.RecycleAll();
            SpawnNpc(npcOnStart);

            TotalScore = 0;
            isGameOver = false;
        }

        void Update()
        {
            if (isGameOver) return;

            shell.UpdateShell();

            for (var i = 0; i < npcBubblePool.SpawnedCount; i++)
                npcBubblePool.Spawned[i].UpdateBubble(shell);

            progressBar.SetProgress(shell.DecreaseProgress);

            if (Input.GetKeyDown(KeyCode.R))
                Restart();
        }

        void OnShellRadiusChange(float radius)
        {
            if (radius <= minShellRadius)
                GameOver();
        }

        void OnNpcRecycled(NpcBubble npc)
        {
            if (isGameOver) return;

            if (!npc.IsEnemy)
                TotalScore += Mathf.RoundToInt(scorePerFriend * shell.Radius);

            SpawnNpc(npcOnRecycle);
        }

        void SpawnNpc(int count)
        {
            var position = Vector3.zero;
            var shellPosition = shell.transform.position;

            for (var i = 0; i < count; i++) {
                var npc = npcBubblePool.SpawnRandom();
                npc.IsEnemy = Random.value >= 0.5f;

                var numTries = 0;
                var range = spawnRange;
                do {
                    numTries++;
                    if (numTries > c_SpawnTriesThreshold)
                        range *= spawnRangeIncr;
                    
                    position.x = shellPosition.x + Random.Range(range.x, range.y);
                    position.y = shellPosition.y + Random.Range(range.x, range.y);
                } while (!IsDistant(position));

                position.z = 0.1f;

                npc.transform.position = position;
            }
        }

        bool IsDistant(Vector3 position)
        {
            var shellDist = Vector3.Distance(position, shell.transform.position);
            if (shellDist < shell.Radius * spawnShellDist)
                return false;

            for (var i = 0; i < npcBubblePool.SpawnedCount; i++) {
                var npc = npcBubblePool.Spawned[i];

                var npcDist = Vector3.Distance(position, npc.transform.position);
                if (npcDist < npc.Radius * spawnNpcDist)
                    return false;
            }

            return true;
        }

        void GameOver()
        {
            isGameOver = true;

            progressBar.Hide();
            gameOverPopup.Show(TotalScore);
        }

        public void Restart()
        {
            isGameOver = true;

            Reset();
        }
    }
}
