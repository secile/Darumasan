using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;

namespace Darumasan
{
    class JumpSensor
    {
        public Action OnJump { get; set; }

        public Action Start = null;
        public Action Stop = null;

        public bool Available { get; private set; }

        public JumpSensor(Context context)
        {
            var sensor = new GravitySensor(context);
            Available = sensor.Available;
            if (Available == false) return;

            sensor.OnNext += x =>
            {
                var a = new OpenTK.Vector3(x.AccelerationX, x.AccelerationY, x.AccelerationZ); // 加速度(重力加速度なし)
                var g = new OpenTK.Vector3(x.GravityX, x.GravityY, x.GravityZ);                // 下向き(重力加速度)

                var length = a.Length;
                //Console.WriteLine($"gravity length:{length}");

                // 加速度が一定以上ならジャンプとする。
                if (length > 15) // m/s^2
                {
                    OnJump?.Invoke();
                }

            };

            Start = () => sensor.Start(SensorDelay.Ui);
            Stop = () => sensor.Stop();
        }
    }

    class GravitySensor
    {
        //private Context mContext;
        private SensorManager mSensorManager;
        private SensorEventListener mSensorEventListener;

        public struct Gravity
        {
            public float GravityX, GravityY, GravityZ;
            public float AccelerationX, AccelerationY, AccelerationZ;
            public Gravity(float gx, float gy, float gz, float ax, float ay, float az)
            {
                this.GravityX = gx;
                this.GravityY = gy;
                this.GravityZ = gz;
                this.AccelerationX = ax;
                this.AccelerationY = ay;
                this.AccelerationZ = az;
            }
        };
        public Action<Gravity> OnNext { get; set; }

        public bool Available { get; private set; }

        public Action<SensorDelay> Start { get; private set; }
        public Action Stop { get; private set; }

        public GravitySensor(Context context)
        {
            //mContext = context;
            mSensorManager = context.GetSystemService(Context.SensorService) as SensorManager;
            mSensorEventListener = new SensorEventListener(this);

            // Accelerometerは重力加速度あり。LinearAccelerationはAccelerometerから重力加速度の影響を取り除いたもの。
            var sensor_lineara = mSensorManager.GetDefaultSensor(SensorType.LinearAcceleration);
            var sensor_gravity = mSensorManager.GetDefaultSensor(SensorType.Gravity);
            if (sensor_lineara == null || sensor_gravity == null)
            {
                return;
            }

            Start = (delay) =>
            {
                mSensorManager.RegisterListener(mSensorEventListener, sensor_lineara, delay);
                mSensorManager.RegisterListener(mSensorEventListener, sensor_gravity, delay);
            };

            Stop = () =>
            {
                mSensorManager.UnregisterListener(mSensorEventListener);
            };

            Available = true;
        }

        /*public bool Start(SensorDelay delay)
        {
            // Accelerometerは重力加速度あり。LinearAccelerationはAccelerometerから重力加速度の影響を取り除いたもの。
            var sensor_lineara = mSensorManager.GetDefaultSensor(SensorType.LinearAcceleration);
            var sensor_gravity = mSensorManager.GetDefaultSensor(SensorType.Gravity);
            if (sensor_lineara == null) return false;
            if (sensor_gravity == null) return false;
            mSensorManager.RegisterListener(mSensorEventListener, sensor_lineara, delay);
            mSensorManager.RegisterListener(mSensorEventListener, sensor_gravity, delay);

            return true;
        }

        public void Stop()
        {
            mSensorManager.UnregisterListener(mSensorEventListener);
        }*/

        private class SensorEventListener : Java.Lang.Object, ISensorEventListener
        {
            private readonly GravitySensor Owner;

            public SensorEventListener(GravitySensor owner)
            {
                Owner = owner;
            }

            // 重力センサ
            private bool GravityFlag = false;
            private bool LinearAccelerationFlag = false;
            private float[] Gravity = new float[3];
            private float[] LinearAcceleration = new float[3];

            public void OnSensorChanged(SensorEvent e)
            {
                // 信頼できない値は利用しない。
                if (e.Accuracy == SensorStatus.NoContact) return;
                if (e.Accuracy == SensorStatus.Unreliable) return;

                // センサ値を取得。GCを避けるためToArray()やToList()は使わない。
                if (e.Sensor.Type == SensorType.Gravity) { e.Values.CopyTo(Gravity, 0); GravityFlag = true; }
                if (e.Sensor.Type == SensorType.LinearAcceleration) { e.Values.CopyTo(LinearAcceleration, 0); LinearAccelerationFlag = true; }
                
                if (e.Sensor.Type == SensorType.LinearAcceleration)
                {
                    if (GravityFlag && LinearAccelerationFlag)
                    {
                        // Gravity：重力加速度ベクトル。√x*x + y*y + z*z = 1G(9.8m/s2)
                        // LinearAcceleration：加速度ベクトル。(重力加速度なし)
                        Owner.OnNext(new Gravity(Gravity[0], Gravity[1], Gravity[2], LinearAcceleration[0], LinearAcceleration[1], LinearAcceleration[2]));
                    }
                }
            }

            public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
            {

            }
        }
    }
}