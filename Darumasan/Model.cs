using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darumasan
{
    class Model
    {
        public Player Player { get; private set; }
        public List<Enemy> Enemies { get; private set; }

        public Action OnGameOver { get; set; }

        public Model()
        {
            Enemies = new List<Enemy>();
            Player = new Player(Enemies);
        }

        public void Action()
        {
            if (GameStatus == GameStatusType.Title)
            {
                SetGameStatus(GameStatusType.Ready);
            }
            else
            {
                Attack();
            }
        }

        private void Attack()
        {
            Player.Attack = true;
        }

        private void InitStage(int stage)
        {
            Player.Init();
            Enemies.Clear();

            var rand = new Random();
            var enemy_num = 1 + stage; // 2,3,4...
            for (int i = 0; i < enemy_num; i++)
            {
                var enemy = new Enemy();
                enemy.Angle = rand.NextDouble() * Math.PI * 2; // 全周囲にランダムで
                enemy.Distance = 0.5 + (rand.NextDouble() * 1.0); // 0.5-1.5範囲でランダム
                enemy.Speed = 0.001 + (rand.NextDouble()  * 0.0001 * stage); // ステージが進むごとに少しずつ早く。
                Enemies.Add(enemy);
            }

            // 敵の1つだけは正面45度の範囲に出現。
            Enemies[0].Angle = (-Math.PI / 8) + (rand.NextDouble() * Math.PI / 4);
            //Enemies[0].Angle = 0;
        }

        private int GameStage;
        private int _GameScore;
        private int GameScore
        {
            get { return _GameScore; }
            set
            {
                _GameScore = value;
                GameScoreUpdated?.Invoke(value);
            }
        }
        public Action<int> GameScoreUpdated;

        public enum GameStatusType { Init, Title, Ready, Game, Clear, Next, Over }
        public GameStatusType GameStatus { get; private set; } = GameStatusType.Init;
        private int StatusTimer;

        private void SetGameStatus(GameStatusType status)
        {
            GameStatus = status;
            StatusTimer = 0;
        }

        public void UpdateStatus()
        {
            switch (GameStatus)
            {
                case GameStatusType.Init:
                    SetGameStatus(GameStatusType.Title);
                    GameStage = 1;
                    InitStage(GameStage);
                    break;

                case GameStatusType.Title:
                    break;

                case GameStatusType.Ready:
                    if (StatusTimer == 0) GameScore = 0;
                    if (StatusTimer > 60) SetGameStatus(GameStatusType.Game);
                    break;

                case GameStatusType.Game:
                    GameMain();
                    break;

                case GameStatusType.Clear:
                    if (StatusTimer > 120) SetGameStatus(GameStatusType.Next);
                    break;

                case GameStatusType.Next:
                    SetGameStatus(GameStatusType.Ready);
                    GameStage += 1;
                    InitStage(GameStage);
                    break;

                case GameStatusType.Over:
                    if (StatusTimer > 240) SetGameStatus(GameStatusType.Init);
                    break;
            }

            StatusTimer++;
        }

        private void GameMain()
        {
            // Playerがやられたらゲームオーバー。
            // Enemyが全滅したら次のステージへ。
            if (Enemies.Count == 0) SetGameStatus(GameStatusType.Clear);
            if(Player.Destroy)
            {
                SetGameStatus(GameStatusType.Over);
                OnGameOver?.Invoke();
            }

            // PlayerとEnemyの状態を更新。
            Player.Update();
            foreach (var enemy in Enemies)
            {
                enemy.Update();
            }

            // 倒したEnemyをリストから削除（要素の削除はリストの逆から。）
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = Enemies[i];
                if (enemy.Destroy)
                {
                    Enemies.Remove(enemy);
                    GameScore = ++GameScore;
                }
            }
        }
    }
}