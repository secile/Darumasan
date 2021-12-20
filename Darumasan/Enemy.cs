using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

namespace Darumasan
{
    class Enemy
    {
        /// <summary>表示位置角度をラジアンで。</summary>
        public double Angle { get; set; }

        /// <summary>中心からの距離。0.0-1.0範囲</summary>
        public double Distance { get; set; }

        /// <summary>移動速度</summary>
        public double Speed { get; set; }

        public bool Destroy { get; set; }

        private Shapes.Shape Shape;

        public Enemy()
        {
            Shape = new Shapes.Sphere(0.5, 16, 16);
        }

        public enum StatusTypes { Wait, Move, Down }
        public StatusTypes Status { get; private set; } = StatusTypes.Wait;

        private int StatusTimer;

        public void SetStatus(StatusTypes status)
        {
            Status = status;
            StatusTimer = 0;
        }

        public void Update()
        {
            switch (Status)
            {
                case StatusTypes.Move:
                    StatusMove();
                    break;

                case StatusTypes.Down:
                    if (StatusTimer > 60) Destroy = true;
                    break;
            }

            StatusTimer++;
        }

        private void StatusMove()
        {
            this.Distance -= this.Speed;
            if (this.Distance < 0) this.Distance = 0;
        }

        public void Draw(Shader shader)
        {
            // [座標系]右手座標系。右手で親指(X軸)・人差指(Y軸)・中指(Z軸)の方向。
            //
            // Y軸(上)
            //     |
            //     |
            //   (* *)
            //  (     )----X軸(右)
            //    /
            //   /
            // Z軸(前)

            if (Status == StatusTypes.Down)
            {
                shader.Rotate(-45, Vector3.UnitX);
                shader.Translate(0, -0.5f, -0.2f); // 後退して少し沈む。
            }

            shader.SetMaterial(Color4.White);

            shader.PushMatrix();
            {
                // 上玉ちょっと小さく。
                shader.Scale(0.7f, 0.7f, 0.7f);
                Shape.Draw(shader);
            }
            shader.PopMatrix();

            shader.PushMatrix();
            {
                // 下玉ちょっと下に移動。
                shader.Translate(0, -0.6f, 0);
                Shape.Draw(shader);
            }
            shader.PopMatrix();

            shader.SetMaterial(Color4.Black);

            shader.PushMatrix();
            {
                // 右目。少し右に寄せて手前(Z軸)に
                shader.Translate(+0.15f, 0.1f, 0.3f);
                shader.Scale(0.1f, 0.1f, 0.1f);
                if (Status == StatusTypes.Wait) shader.Scale(1, 0.2f, 1); // 待機中は目を閉じる。
                Shape.Draw(shader);
            }
            shader.PopMatrix();

            shader.PushMatrix();
            {
                // 左目。少し左に寄せて手前(Z軸)に
                shader.Translate(-0.15f, 0.1f, 0.3f);
                shader.Scale(0.1f, 0.1f, 0.1f);
                if (Status == StatusTypes.Wait) shader.Scale(1, 0.2f, 1); // 待機中は目を閉じる。
                Shape.Draw(shader);
            }
            shader.PopMatrix();
        }
    }
}
