using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darumasan
{
    class Vibrator
    {
        public bool Available { get; private set; }

        private Android.OS.Vibrator device;
        public Vibrator(Context context)
        {
            device = context.GetSystemService(Context.VibratorService) as Android.OS.Vibrator;
            Available = device != null;
        }

        public void OneShot()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var effect = VibrationEffect.CreateOneShot(200, VibrationEffect.DefaultAmplitude);
                device.Vibrate(effect);
            }
            else
            {
                device.Vibrate(200);
            }
        }
    }
}