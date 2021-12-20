using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace Darumasan
{
    class Player
    {
        private IEnumerable<Enemy> Enemies;
        public Player(IEnumerable<Enemy> enemies)
        {
            this.Enemies = enemies;

            Line = new Shapes.Line(Vector3.Zero, Vector3.UnitX);
            Sphere = new Shapes.Sphere(0.2f, 16, 16);
        }

        private static readonly Vector3 ForwardBase = -Vector3.UnitZ; // 前向き基準は-Z軸。
        public Vector3 Forward
        {
            get { return _Forward; }
            set
            {
                // ジャンプ中に上下に視線が向いて敵が動いてやられてしまうため
                // 上下方向は向かない（水平方向だけ向くように）
                _Forward = value;
                _Forward.Y = 0;
                _Forward.Normalize();
            }
        }
        private Vector3 _Forward = ForwardBase;

        public void Rotate(Matrix4 mat)
        {
            Forward = Vector3.TransformVector(ForwardBase, mat);
        }

        public void Init()
        {
            Destroy = false;
            Attack = false;
        }

        public bool Attack { get; set; }

        public bool Destroy { get; set; }

        private Shapes.Line Line;
        private Shapes.Sphere Sphere;

        public void Draw(Shader shader)
        {
            // [座標系]右手座標系。右手で親指(X軸)・人差指(Y軸)・中指(Z軸)の方向。
            //
            // Y軸(上)
            //     |
            //     |
            //     |
            //     +-------X軸(右)
            //    /
            //   /
            // Z軸(前)

            GL.LineWidth(5);
            shader.SetMaterial(Color4.Green);

            shader.PushMatrix();
            {
                // 前向きベクトルを描画。
                Line.Update(Vector3.Zero, Forward);
                Line.Draw(shader);
                Sphere.Draw(shader);
            }
            shader.PopMatrix();
        }

        public void Update()
        {
            // すべての敵を近い順にチェック。
            foreach (var enemy in Enemies.OrderBy(x => x.Distance))
            {
                if (enemy.Status == Enemy.StatusTypes.Down) continue;

                // 敵との衝突判定。
                if (enemy.Distance < 0.1)
                {
                    Destroy = true;
                }

                // 正面なら敵は移動不可。
                if (IsSameAngle(this.Forward, enemy.Angle))
                {
                    enemy.SetStatus(Enemy.StatusTypes.Wait);

                    if (Attack)
                    {
                        if (enemy.Distance < 0.3)
                        {
                            enemy.SetStatus(Enemy.StatusTypes.Down);
                            break;
                        }
                    }
                }
                else
                {
                    enemy.SetStatus(Enemy.StatusTypes.Move);
                }
            }

            Attack = false;
        }

        /// <summary>Playerの向きとEnemyの方向が同じ場合true。</summary>
        private bool IsSameAngle(Vector3 vec_a, double angle)
        {
            var mat = Matrix4.CreateFromAxisAngle(Vector3.UnitY, (float)angle);
            var vec_b = Vector3.TransformVector(ForwardBase, mat);
            var dot = Vector3.Dot(vec_a, vec_b);
            return dot > 0.95; // 同じ向きなら内積が1.0
        }
    }
}
