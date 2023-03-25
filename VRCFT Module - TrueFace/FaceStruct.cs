using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCFT_Module_TrueFace
{
    internal class FaceStruct
    {
    }
    public class TrueFaceTrackingDataLips
    {
        public float jawForward;
        public float jawLeft;
        public float jawRight;
        public float jawOpen;
        public float mouthClose;
        public float mouthFunnel;
        public float mouthPucker;
        public float mouthLeft;
        public float mouthRight;
        public float mouthSmile_L;
        public float mouthSmile_R;
        public float mouthFrown_L;
        public float mouthFrown_R;
        public float mouthDimple_L;
        public float mouthDimple_R;
        public float mouthStretch_L;
        public float mouthStretch_R;
        public float mouthRollLower;
        public float mouthRollUpper;
        public float mouthShrugLower;
        public float mouthShrugUpper;
        public float mouthPress_L;
        public float mouthPress_R;
        public float mouthLowerDown_L;
        public float mouthLowerDown_R;
        public float mouthUpperUp_L;
        public float mouthUpperUp_R;
        public float cheekPuff;
        public float cheekSquint_L;
        public float cheekSquint_R;
        public float noseSneer_L;
        public float noseSneer_R;
        public float tongueOut;
    }
    public class TrueFaceTrackingDataStruct
    {
        // We only need to use the data for the mouth tracking, so don't worry about the eyes
        public TrueFaceTrackingDataLips lips = new TrueFaceTrackingDataLips();
        public void ProcessMouth(Dictionary<string, float> mouthData)
        {
            foreach (var shapes in typeof(TrueFaceTrackingDataLips).GetFields())
            {
                shapes.SetValue(lips, mouthData[shapes.Name]);

            }

        }
    }
    public static class Constants
    {
        // The proper names of each ARKit blendshape
        // hey guess what live link developer THIS IS ACTUALLY NOT HOW THEY'RE REALLY NAMED GOAHJNDFUIOWAHDUIYAWHUIDHAWUHDWAUIHDAWUI
        // i have spent the last 2 hours trying to figure out why my code wasn't working and it's because of this
        public static readonly string[] TrueFaceNames = {
            "eyeBlink_L",
            "eyeLookDown_L",
            "eyeLookIn_L",
            "eyeLookOut_L",
            "eyeLookUp_L",
            "eyeSquint_L",
            "eyeWide_L",
            "eyeBlink_R",
            "eyeLookDown_R",
            "eyeLookIn_R",
            "eyeLookOut_R",
            "eyeLookUp_R",
            "eyeSquint_R",
            "eyeWide_R",
            "jawForward",
            "jawLeft",
            "jawRight",
            "jawOpen",
            "mouthClose",
            "mouthFunnel",
            "mouthPucker",
            "mouthLeft",
            "mouthRight",
            "mouthSmile_L",
            "mouthSmile_R",
            "mouthFrown_L",
            "mouthFrown_R",
            "mouthDimple_L",
            "mouthDimple_R",
            "mouthStretch_L",
            "mouthStretch_R",
            "mouthRollLower",
            "mouthRollUpper",
            "mouthShrugLower",
            "mouthShrugUpper",
            "mouthPressLeft",
            "mouthPressRight",
            "mouthLowerDownLeft",
            "mouthLowerDownRight",
            "mouthUpperUpLeft",
            "mouthUpperUpRight",
            "browDown_L",
            "browDown_R",
            "browInnerUp",
            "browOuterUp_L",
            "browOuterUp_R",
            "cheekPuff",
            "cheekSquint_L",
            "cheekSquint_R",
            "noseSneer_L",
            "noseSneer_R",
            "tongueOut",
            "headYaw",
            "headPitch",
            "headRoll",
            "EyeYawLeft", // LeftEyeYaw // leaving these here because they're not even listed in my blendshape list LOL
            "EyePitchLeft", // LeftEyePitch
            "EyeRollLeft", // LeftEyeRoll
            "EyeYawRight", // RightEyeYaw
            "EyePitchRight", // RightEyePitch
            "EyeRollRight"}; // RightEyeRoll

        public static int Port = 4863;
    }
}
