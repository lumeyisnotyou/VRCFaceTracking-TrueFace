using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using ViveSR.anipal.Lip;
using VRCFaceTracking;
using VRCFaceTracking.Params;

// Credit to the livelink module, it's basically ARKit, so I'm reading the code as I write this 💀
namespace VRCFT_Module_TrueFace
{
    // This class contains the overrides for any VRCFT Tracking Data struct functions
    public static class TrackingData
    {
        // Map the JawOpen and MouthClose LiveLink blendshapes to the apeShape SRanipal blendshape
        private static float ApeCalc(float jawOpen, float mouthClose) => (0.05f + jawOpen) * (float)Math.Pow(0.05 + mouthClose, 2);

        // Map the TrueFace module's lip tracking data to the SRanipal API
        private static void Update(ref LipTrackingData data, TrueFaceTrackingDataLips external)
        {
            //if (!UnifiedLibManager.LipEnabled) return;

            Dictionary<LipShape_v2, float> lipShapes = new Dictionary<LipShape_v2, float> {
                { LipShape_v2.JawRight, external.jawRight }, // +JawX
                { LipShape_v2.JawLeft, external.jawLeft }, // -JawX
                { LipShape_v2.JawForward, external.jawForward },
                { LipShape_v2.JawOpen, external.jawOpen },
                { LipShape_v2.MouthApeShape, ApeCalc(external.jawOpen, external.mouthClose) },
                { LipShape_v2.MouthUpperRight, external.mouthRight }, // +MouthUpper
                { LipShape_v2.MouthUpperLeft, external.mouthLeft }, // -MouthUpper
                { LipShape_v2.MouthLowerRight, external.mouthRight }, // +MouthLower
                { LipShape_v2.MouthLowerLeft, external.mouthLeft }, // -MouthLower
                { LipShape_v2.MouthUpperOverturn, external.mouthShrugUpper },
                { LipShape_v2.MouthLowerOverturn, external.mouthShrugLower },
                { LipShape_v2.MouthPout, (external.mouthFunnel + external.mouthPucker) / 2 },
                { LipShape_v2.MouthSmileRight, external.mouthSmile_R }, // +SmileSadRight
                { LipShape_v2.MouthSmileLeft, external.mouthSmile_L }, // +SmileSadLeft
                { LipShape_v2.MouthSadRight, external.mouthFrown_R }, // -SmileSadRight
                { LipShape_v2.MouthSadLeft, external.mouthFrown_L }, // -SmileSadLeft
                { LipShape_v2.CheekPuffRight, external.cheekPuff },
                { LipShape_v2.CheekPuffLeft, external.cheekPuff },
                { LipShape_v2.CheekSuck, 0 },
                { LipShape_v2.MouthUpperUpRight, external.mouthUpperUp_R },
                { LipShape_v2.MouthUpperUpLeft, external.mouthUpperUp_R },
                { LipShape_v2.MouthLowerDownRight, external.mouthUpperUp_R },
                { LipShape_v2.MouthLowerDownLeft, external.mouthUpperUp_R },
                { LipShape_v2.MouthUpperInside, external.mouthUpperUp_R },
                { LipShape_v2.MouthLowerInside, external.mouthUpperUp_R },
                { LipShape_v2.MouthLowerOverlay, 0 },
                { LipShape_v2.TongueLongStep1, external.tongueOut },
                { LipShape_v2.TongueLeft, 0 }, // -TongueX
                { LipShape_v2.TongueRight, 0 }, // +TongueX
                { LipShape_v2.TongueUp, 0 }, // +TongueY
                { LipShape_v2.TongueDown, 0 }, // -TongueY
                { LipShape_v2.TongueRoll, 0 },
                { LipShape_v2.TongueLongStep2, external.tongueOut },
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

        // Map the LiveLink module's full data to the SRanipal API
        public static void Update(TrueFaceTrackingDataLips external)
        {
            Update(ref UnifiedTrackingData.LatestLipData, external);
        }
    }


    // Connect to the external tracking system here. The connection is a TCP socket on port 4863.
    public class TrueFaceTrackingModule : ExtTrackingModule
    {
        private IPAddress localAddr;
        private const int Port = 4863;

        private NetworkStream stream;
        private TcpClient client;
        private TcpListener listener;
        private CancellationTokenSource cancellationToken;
        private bool connected = false;
        private TrueFaceTrackingDataLips _latestData;

        public override (bool SupportsEye, bool SupportsLip) Supported => (false, true);

        public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            Logger.Msg("Initializing TrueFace");

            Logger.Msg("Initializing inside external module");
            Logger.Msg("Opening port to external tracking system.");

            try
            {
                localAddr = IPAddress.Any;
                cancellationToken = new CancellationTokenSource();
                // Start listening for connections
                
                Logger.Msg("Started listener");
                ConnectToTCP();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return (false, false);
            }

            _latestData = new TrueFaceTrackingDataLips();
            Logger.Msg("Port opened.");
            return Supported;
        }

        private bool ConnectToTCP()
        {
            while (true)
            {

                // Start the listener and wait for a client
                listener = new TcpListener(localAddr, Port);
                listener.Start();
                Logger.Msg("Waiting for a client");
                client = listener.AcceptTcpClient();
                stream = client.GetStream();
                Logger.Msg("Stream recieved!");
                Console.WriteLine(stream);
                while (client.Connected)
                {
                    connected = true;
                    Logger.Msg("Connected to external tracking system.");
                    Logger.Msg(stream.ToString());
                    return true;
                }
                
            }
            }

        // This will be run in the tracking thread. This is exposed so you can control when and if the tracking data is updated down to the lowest level.
        public override Action GetUpdateThreadFunc()
        {
            return () =>
            {
                while (!cancellationToken.IsCancellationRequested)
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
                // Attempt reconnection if needed
                if (!connected || stream == null)
                {
                    Logger.Warning("The connection was a LIE!");
                    ConnectToTCP();
                }

                // If the connection was unsuccessful, wait a bit and try again
                if (stream == null)
                {
                    Logger.Warning("Didn't reconnect just yet! Trying again...");
                    return;
                }

                if (!stream.CanRead)
                {
                    Logger.Warning("Can't read from the network stream just yet! Trying again...");
                    return;
                }

                /*List<byte> byteStream = new List<byte>();
                int @byte = stream.ReadByte();
                while (@byte != -1)
                {
                    Console.WriteLine("Adding" + @byte);
                    byteStream.Add((byte)@byte);
                    @byte = stream.ReadByte();
                }*/
                // Read the data from the stream, compiling the full payload, which may be seperated over multiple packets
                byte[] buffer = new byte[client.ReceiveBufferSize];
                MemoryStream byteStream = new MemoryStream();
                int bytesRead = 0;
                do
                {
                    bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                    byteStream.Write(buffer, 0, bytesRead);
                } while (stream.DataAvailable);

                /*if (connected)
                {
                    Logger.Warning("End of stream! Reconnecting...");
                    Thread.Sleep(1000);
                    connected = false;
                    try
                    {
                        stream.Close();
                    }
                    catch (SocketException e)
                    {
                        Logger.Error(e.Message);
                        Thread.Sleep(1000);
                    }
                }*/

                Console.WriteLine("Received data from external tracking system.");
                // Parse the data into a VRCFT-Parseable format
                try
                {
                    ReadData(byteStream.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                // Update the tracking data
                TrackingData.Update(_latestData);
                // Print the data to the console, just to make sure it's working
                Logger.Msg("Received data from external tracking system.");
                // Print the lip data from the external tracking system
            }
            catch (Exception e)
            {
                Logger.Msg(e.ToString());
            }
        }

        // A chance to de-initialize everything. This runs synchronously inside main game thread. Do not touch any Unity objects here.

        public override void Teardown()
        {
            cancellationToken?.Cancel();
            stream.Close();
            stream.Dispose();
            listener.Stop();
            //client?.Close();
            //client.Dispose();
            cancellationToken?.Dispose();
        }

        private void ReadData(byte[] data)
        {
            // Read the data from the external tracking system, which is in JSON
            // The data is in the format of a TrueFaceTrackingDataStruct
            try
            {
                var trackingData = new TrueFaceTrackingDataLips();
                var json = System.Text.Encoding.UTF8.GetString(data);
                // Parse the JSON into a TrueFaceTrackingDataStruct
                trackingData = JsonConvert.DeserializeObject<TrueFaceTrackingDataLips>(json);
                Console.WriteLine(trackingData.jawOpen);
                // Update the latest data
                _latestData = trackingData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}