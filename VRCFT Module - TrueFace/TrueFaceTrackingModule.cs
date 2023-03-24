using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ViveSR.anipal.Lip;
using VRCFaceTracking;
using VRCFaceTracking.Params;

// Credit to the livelink module, it's basically ARKit, so i'm reading the code as I write this 💀

namespace VRCFT_Module_TrueFace
{


    // This class contains the overrides for any VRCFT Tracking Data struct functions
    public static class TrackingData
    {


        // Map the JawOpen and MouthClose LiveLink blendshapes to the apeShape SRanipal blendshape
        private static float apeCalc(float jawOpen, float mouthClose) => (0.05f + jawOpen) * (float)Math.Pow(0.05 + mouthClose, 2);

        // Map the LiveLink module's lip tracking data to the SRanipal API
        private static void Update(ref LipTrackingData data, TrueFaceTrackingDataLips external)
        {
            //if (!UnifiedLibManager.LipEnabled) return;

            Dictionary<LipShape_v2, float> lipShapes = new Dictionary<LipShape_v2, float>{
                    { LipShape_v2.JawRight, external.JawRight }, // +JawX
                    { LipShape_v2.JawLeft, external.JawLeft }, // -JawX
                    { LipShape_v2.JawForward, external.JawForward },
                    { LipShape_v2.JawOpen, external.JawOpen },
                    { LipShape_v2.MouthApeShape, apeCalc(external.JawOpen, external.MouthClose) },
                    { LipShape_v2.MouthUpperRight, external.MouthRight }, // +MouthUpper
                    { LipShape_v2.MouthUpperLeft, external.MouthLeft }, // -MouthUpper
                    { LipShape_v2.MouthLowerRight, external.MouthRight }, // +MouthLower
                    { LipShape_v2.MouthLowerLeft, external.MouthLeft }, // -MouthLower
                    { LipShape_v2.MouthUpperOverturn, external.MouthShrugUpper },
                    { LipShape_v2.MouthLowerOverturn, external.MouthShrugLower },
                    { LipShape_v2.MouthPout, (external.MouthFunnel + external.MouthPucker) / 2 },
                    { LipShape_v2.MouthSmileRight, external.MouthSmileRight }, // +SmileSadRight
                    { LipShape_v2.MouthSmileLeft, external.MouthSmileLeft }, // +SmileSadLeft
                    { LipShape_v2.MouthSadRight, external.MouthFrownRight }, // -SmileSadRight
                    { LipShape_v2.MouthSadLeft, external.MouthFrownLeft }, // -SmileSadLeft
                    { LipShape_v2.CheekPuffRight, external.CheekPuff },
                    { LipShape_v2.CheekPuffLeft, external.CheekPuff },
                    { LipShape_v2.CheekSuck, 0 },
                    { LipShape_v2.MouthUpperUpRight, external.MouthUpperUpRight },
                    { LipShape_v2.MouthUpperUpLeft, external.MouthUpperUpLeft },
                    { LipShape_v2.MouthLowerDownRight, external.MouthLowerDownRight },
                    { LipShape_v2.MouthLowerDownLeft, external.MouthLowerDownLeft },
                    { LipShape_v2.MouthUpperInside, external.MouthRollUpper },
                    { LipShape_v2.MouthLowerInside, external.MouthRollLower },
                    { LipShape_v2.MouthLowerOverlay, 0 },
                    { LipShape_v2.TongueLongStep1, external.TongueOut },
                    { LipShape_v2.TongueLeft, 0 }, // -TongueX
                    { LipShape_v2.TongueRight, 0 }, // +TongueX
                    { LipShape_v2.TongueUp, 0 }, // +TongueY
                    { LipShape_v2.TongueDown, 0 }, // -TongueY
                    { LipShape_v2.TongueRoll, 0 },
                    { LipShape_v2.TongueLongStep2, external.TongueOut },
                    { LipShape_v2.TongueUpRightMorph, 0 },
                    { LipShape_v2.TongueUpLeftMorph, 0 },
                    { LipShape_v2.TongueDownRightMorph, 0 },
                    { LipShape_v2.TongueDownLeftMorph, 0 },
                };

            for (int i = 0; i < SRanipal_Lip_v2.WeightingCount; i++)
            {
                data.LatestShapes[i] = lipShapes.Values.ElementAt(i);
            }
        }

        // Map the LiveLink module's eye data to the SRanipal API

        // Map the LiveLink module's full data to the SRanipal API
        public static void Update(TrueFaceTrackingDataStruct external)
        {
            Update(ref UnifiedTrackingData.LatestLipData, external.lips);
        }
    }


    // Connect to the external tracking system here. The connection is a UDP socket on port 4863.
    public class TrueFaceTrackingModule : ExtTrackingModule
    {
        private static CancellationTokenSource _cancellationToken;
        private UdpClient _client;
        private IPEndPoint _EndPoint;
        private TrueFaceTrackingDataStruct _latestData;
        public override (bool SupportsEye, bool SupportsLip) Supported => (false, true);

        public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            _cancellationToken?.Cancel();
            Console.WriteLine("Initializing inside external module");
            Console.WriteLine("Opening port to external tracking system.");
            
            _client = new UdpClient(4863);
            _EndPoint = new IPEndPoint(IPAddress.Any, 4863);
            _latestData = new TrueFaceTrackingDataStruct();
            Console.WriteLine("Port opened.");
            return (false, true);
        }

        // This will be run in the tracking thread. This is exposed so you can control when and if the tracking data is updated down to the lowest level.
        public override Action GetUpdateThreadFunc()
        {
            _cancellationToken = new CancellationTokenSource();
            return () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                   
                    Update();
                    Thread.Sleep(10);
                }
            };
        }

        // The update function needs to be defined separately in case the user is running with the --vrcft-nothread launch parameter
        public void Update()
        {
            Console.WriteLine("Updating inside external module.");
            // Receive the data from the external tracking system
            try
            {
                byte[] data = _client.Receive(ref _EndPoint);
                Console.WriteLine("Received data from external tracking system.");
                // Parse the data into a VRCFT-Parseable format
                ReadData(data);
                // Update the tracking data
                TrackingData.Update(_latestData);
                // Print the data to the console, just to make sure it's working
                Console.WriteLine("Received data from external tracking system.");
                // Print the lip data from the external tracking system
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            

            Console.WriteLine("Jaw Open: " + _latestData.lips.JawOpen);
            if (Status.EyeState == ModuleState.Active)
                Console.WriteLine("Eye data is being utilized.");
            if (Status.LipState == ModuleState.Active)
                Console.WriteLine("Lip data is being utilized.");
        }

        // A chance to de-initialize everything. This runs synchronously inside main game thread. Do not touch any Unity objects here.



        public override void Teardown()
        {
            Console.WriteLine("Teardown");
        }
        private void ReadData(byte[] data)
        {
            // Read the data from the external tracking system, which is in JSON
            var trackingData = new TrueFaceTrackingDataStruct();
            var json = System.Text.Encoding.UTF8.GetString(data);
            // Parse the data into a VRCFT-Parseable format 
            trackingData = JsonConvert.DeserializeObject<TrueFaceTrackingDataStruct>(json);
            // Print the data to the console, just to make sure it's working
            Console.WriteLine("Received data from external tracking system.");
            // Print the lip data from the external tracking system
            Console.WriteLine("Jaw data: " + trackingData.lips.JawOpen);
            // Set the latest data to the data we just received
            _latestData = trackingData;

        }
    }
}