using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved
{
    public class Randomizer
    {
        public const int Seed = 5366354;

        public int Index;

        private bool isInitialized = false;
        //private float currentAngle;

        private float[] RandomSet = new float[128];
        private float[] RandomSetFromAngle = new float[128];

        public void Initialize(long EntityId, float maxAngle)
        {
            Random rand = new Random(Seed);

            char[] id = EntityId.ToString().ToCharArray();

            char[] indexValues = new char[2]
            {
                id[id.Length-1],
                id[id.Length-2]
            };

            Index = int.Parse(new string(indexValues));

            for (int i = 0; i < 128; i++)
            {
                RandomSet[i] = (float)(rand.NextDouble() * 6.28318548f);
            }

            for (int i = 0; i < 128; i++)
            {
                RandomSetFromAngle[i] = (float)(rand.NextDouble() * (maxAngle*2) - maxAngle);
            }

            //currentAngle = maxAngle;

            isInitialized = true;
        }

        public Vector3 ApplyDeviation(IMyEntity entity, Vector3 direction, float maxAngle)
        {
            if (maxAngle == 0)
            {
                return direction;
            }

            if (!isInitialized)
            {
                Initialize(entity.EntityId, maxAngle);
                //MyAPIGateway.Utilities.ShowNotification($"Initialized: {Index}");
            }

            Index++;
            if (Index == 128)
            {
                Index = 0;
            }

            Matrix matrix = Matrix.CreateFromDir(direction);

            float randomFloat = RandomSetFromAngle[Index];
            float randomFloat2 = RandomSet[Index];

            //MyAPIGateway.Utilities.ShowNotification($"Random Values: {randomFloat}  |  {randomFloat2}");

            Vector3 normal = -new Vector3(MyMath.FastSin(randomFloat) * MyMath.FastCos(randomFloat2), MyMath.FastSin(randomFloat) * MyMath.FastSin(randomFloat2), MyMath.FastCos(randomFloat));
            Vector3 vector = Vector3.TransformNormal(normal, matrix);

            //MyAPIGateway.Utilities.ShowNotification($"Angle: {maxAngle} Random Values: {normal.X}  |  {normal.Y}  |  {normal.Z}");

            return vector;
        }
    }
}
