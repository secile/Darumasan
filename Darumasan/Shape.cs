using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace Darumasan.Shapes
{
    /// <summary>図形の抽象クラス。</summary>
    public abstract class Shape
    {
        protected float[] Vertexs;
        protected float[] Normals;
        protected float[] Texture;

        protected BeginMode PrimitiveType = BeginMode.Points;

        /// <summary>頂点数。三角形は「3」。四角形は「4」。</summary>
        protected int VertexCount;

        public virtual void Draw(Shader shader)
        {
            // 頂点座標をシェーダーに
            if (Vertexs != null) shader.SetVertex(Vertexs);
            //if (Normals != null) shader.SetNormal(Normals);
            //if (Texture != null) shader.SetTexture(Texture);

            //描画をシェーダに指示
            shader.UpdateMatrix();
            GL.DrawArrays(PrimitiveType, 0, VertexCount);
        }
    }

    /// <summary>直線をXY平面上に描画する。</summary>
    public class Line : Shape
    {
        public Line(Vector3 a, Vector3 b)
        {
            Vertexs = new float[]
            {
                    a.X, a.Y, a.Z,
                    b.X, b.Y, b.Z
            };

            this.VertexCount = 2;
            this.PrimitiveType = BeginMode.Lines;
        }

        public void Update(Vector3 a, Vector3 b)
        {
            Vertexs[0] = a.X;
            Vertexs[1] = a.Y;
            Vertexs[2] = a.Z;
            Vertexs[3] = b.X;
            Vertexs[4] = b.Y;
            Vertexs[5] = b.Z;
        }
    }

    /// <summary>三角形をXY平面上に描画する。</summary>
    public class Triangle : Shape
    {
        public Triangle(float size)
        {
            Init(size);

            this.VertexCount = 3;
            this.PrimitiveType = BeginMode.Triangles;
        }

        private void Init(float size)
        {
            var s = size / 2;
            Vertexs = new float[]
            {
                +0, +s, +0,
                -s, -s, +0,
                +s, -s, +0
            };

            Normals = new float[]
            {
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f
            };

            Texture = new float[]
            {
                0.0f, 0.5f,
                0.0f, 0.0f,
                1.0f, 0.0f
            };
        }
    }

    /// <summary>四角形をXY平面上に描画する。</summary>
    public class Rectangle : Shape
    {
        public Rectangle(float size)
        {
            Init(size);

            this.VertexCount = 4;
            this.PrimitiveType = BeginMode.TriangleStrip;
        }

        private void Init(float size)
        {
            var s = size / 2;
            Vertexs = new float[]
            {
                -s, +s, +0,
                -s, -s, +0,
                +s, +s, +0,
                +s, -s, +0
            };

            Normals = new float[]
            {
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f
            };

            Texture = new float[]
            {
                0.0f, 1.0f,
                0.0f, 0.0f,
                1.0f, 1.0f,
                1.0f, 0.0f
            };
        }
    }

    /// <summary>球をたぶんY軸方向を上にして描画する。</summary>
    public class Sphere : Shape
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius">半径。</param>
        /// <param name="slices">緯度方向の分割数。</param>
        /// <param name="stacks">経度方向の分割数。</param>
        public Sphere(double radius, int slices, int stacks)
        {
            Init(radius, slices, stacks);

            this.PrimitiveType = BeginMode.TriangleStrip;
            this.VertexCount = Vertexs.Length / 3;
        }

        // 床井研究室 第10回 球を三角形で描く。
        // ただしこの方法はindexバッファを指定する必要がある。
        // indexバッファは利用してないため、今回は利用できない。
        private void InitX(double radius, int slices, int stacks)
        {
            // slice=3, stack=2の場合、
            // 頂点数:(slice+1)×(stack+1)＝12
            // 三角数:(slice×stack×2)=12
            // 
            // ３─２─１─０
            // │／│／│／│ 
            // ７─６─５─４
            // │／│／│／│
            // Ｂ─Ａ─９─８
            // 
            // +--+---+---+---+-------+
            // |  | x | y | z |  三角 |
            // +--+---+---+---+-------+
            // | 0| 3 | 2 | 0 | 0-1-5 |
            // | 1| 2 | 2 | 0 | 0-5-4 |
            // | 2| 1 | 2 | 0 | 1-2-6 |
            // | 3| 0 | 2 | 0 | 1-6-5 |
            // | 4| 3 | 1 | 0 | 2-3-7 |
            // | 5| 2 | 1 | 0 | 2-7-6 |
            // | 6| 1 | 1 | 0 | 4-5-9 |
            // | 7| 0 | 1 | 0 | 4-9-8 |
            // | 8| 3 | 2 | 0 | 5-6-A |
            // | 9| 2 | 2 | 0 | 5-A-9 |
            // |10| 1 | 2 | 0 | 6-7-B |
            // |11| 0 | 2 | 0 | 6-B-A |
            // +--+---+---+---+-------+
            // 
            // 0-1-5の三角形を描く。
            // 0-5-4の三角形を描く。これで最初の四角形が描画できた。
            // 1-2-6の三角形を描く。
            // 1-6-5の三角形を描く。これでつぎの四角形が描画できた。これを繰り返す。

            var vtx = new List<float>();
            var nom = new List<float>();
            var tex = new List<float>();

            for (int i = 0; i <= stacks; i++)
            {
                //輪切り上部
                double ph = Math.PI / stacks * i;
                double y = Math.Cos(ph);
                double r = Math.Sin(ph);

                for (int j = 0; j <= slices; j++)
                {
                    //輪切りの面を単位円としたときの座標
                    double th = 2 * Math.PI / slices * j;
                    double x = Math.Cos(th);
                    double z = Math.Sin(th);

                    vtx.Add((float)(x * r)); // x
                    vtx.Add((float)(y));     // y
                    vtx.Add((float)(z * r)); // z

                    nom.Add((float)(x * 1)); // x
                    nom.Add((float)(y));     // y
                    nom.Add((float)(z * 1)); // z
                }
            }

            Vertexs = vtx.ToArray();
            Normals = nom.ToArray();
            Texture = tex.ToArray();
        }

        // 床井研究室の方法と違って、
        // 同じ頂点を重複するため無駄が多いが
        // indexバッファ無しで描画できる。
        private void Init(double radius, int slices, int stacks)
        {
            var vtx = new List<float>();
            var nom = new List<float>();
            var tex = new List<float>();

            // ３─２─１─０
            // │／│／│／│ 
            // ７─６─５─４
            // │／│／│／│
            // Ｂ─Ａ─９─８
            //
            // 4-0,5-1,6-2,7-3：4-0-5と0-1-5。5-1-6と1-2-6。6-2-7と2-3-7。までの四角形が描画できた。
            // 8-4,9-5,A-6,B-7：8-4-9と4-5-9。9-5-6と5-6-A。A-6-Bと6-7-B。までの四角形が描画できた。これを繰り返す。

            var r = radius;
            for (int i = 0; i < stacks; i++) // i <= stacksではない。
            {
                // 上列(i=0のとき、0-1-2-3列)
                var upperI = i + 0;
                var upperP = Math.PI / stacks * upperI;
                var upperH = Math.Cos(upperP);
                var upperW = Math.Sin(upperP);

                // 下列(i=0のとき、4-5-6-7列)
                var lowerI = i + 1;
                var lowerP = Math.PI / stacks * lowerI;
                var lowerH = Math.Cos(lowerP);
                var lowerW = Math.Sin(lowerP);

                for (int j = 0; j <= slices; j++)
                {
                    //輪切りの面を単位円としたときの座標
                    var w = j / (float)slices; // slices=8のとき1/8, 2/8, 3/8…
                    var O = 2 * Math.PI * w;   // slices=8のとき1/4π,2/4π,3/4π…。θ(シータ)の代わりにOを使う。
                    var x = Math.Cos(O);
                    var y = Math.Sin(O);

                    // 下を先に
                    vtx.Add((float)(x * lowerW * r)); // x
                    vtx.Add((float)(lowerH * r));     // y
                    vtx.Add((float)(y * lowerW * r)); // z

                    nom.Add((float)(x * lowerW));     // x
                    nom.Add((float)(lowerH));         // y
                    nom.Add((float)(y * lowerW));     // z

                    // texはsliceとstackの場所から算出。
                    var ltx = 1.0f - w;                        // slice=8のとき、8/8, 7/8, 6/8…
                    var lty = 1.0f - (lowerI / (float)stacks); // stack=3のとき、0.66, 0.33, 0。
                    tex.Add(ltx);
                    tex.Add(lty);

                    // 上は後に
                    vtx.Add((float)(x * upperW * r)); // x
                    vtx.Add((float)(upperH * r));     // y
                    vtx.Add((float)(y * upperW * r)); // z

                    nom.Add((float)(x * upperW));     // x
                    nom.Add((float)(upperH));         // y
                    nom.Add((float)(y * upperW));     // z

                    // texはsliceとstackの場所から算出。
                    var utx = 1.0f - w;                        // slice=8のとき、8/8, 7/8, 6/8…
                    var uty = 1.0f - (upperI / (float)stacks); // stack=3のとき、1.0, 0.66, 0.33。 
                    tex.Add(utx);
                    tex.Add(uty);
                }
            }

            Vertexs = vtx.ToArray();
            Normals = nom.ToArray();
            Texture = tex.ToArray();
        }
    }
}
