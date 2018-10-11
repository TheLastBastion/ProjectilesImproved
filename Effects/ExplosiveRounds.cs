using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class ExplosiveRounds : EffectBase
    {
        [ProtoMember(2)]
        public float Radius { get; set; }

        [ProtoMember(3)]
        public bool AffectVoxels { get; set; }

        //public ExplosiveRounds()
        //{
        //}

        private List<Vector3I> baseSphere;
        private Matrix epicenterMatrix;
        public int radiusInSmall = 10;
        public float rayDamage = 100;
        float tempDmgPool;

        //Vector3I tempDiffCheck;
        Vector3I tempTargetPoint = new Vector3I();

        public static List<Vector3I> BoomSphere(int radius)
        {
            List<Vector3I> sphere8th = new List<Vector3I>();

            int overallRadius = radius;
            int layerZ = 0;
            int calcR = overallRadius;

            while (layerZ <= overallRadius)
            {
                int x = 0;
                calcR = (int)Math.Round(Math.Cos(layerZ));
                while (x <= calcR)
                {
                    int y = (int)Math.Round(Math.Cos(x));
                    sphere8th.Add(new Vector3I(x, y, layerZ));
                    x++;
                }
                layerZ++;
            }

            List<Vector3I> dmgSphere = new List<Vector3I>();
            dmgSphere.Add(new Vector3I(0, 0, 0));

            for (int i = 0; i<sphere8th.Count; i++)
            {
                Vector3I vec = sphere8th[i];
                dmgSphere.Add(vec);
                dmgSphere.Add(-vec);

                vec.X = -vec.X;
                dmgSphere.Add(vec);
                dmgSphere.Add(-vec);

                vec.Y = -vec.Y;
                dmgSphere.Add(vec);
                dmgSphere.Add(-vec);

                vec.X = -vec.X;
                dmgSphere.Add(vec);
                dmgSphere.Add(-vec);
            }

            return dmgSphere;
        }

        private List<Vector3I> ExplosionToWorld(Matrix exMatrix)
        {
            List<Vector3I> calcList = new List<Vector3I>();

            foreach (Vector3I vec in baseSphere)
            {
                Vector3I calcVec = vec;

                //Get world position of the local origin
                Vector3D referenceWorldPosition = exMatrix.Translation;

                //convert the local vector to a world direction vector
                Vector3D worldDirection = Vector3D.TransformNormal(calcVec, exMatrix);

                //Combine origin position and world direction
                Vector3D worldPosition = referenceWorldPosition + worldDirection;

                calcList.Add(Vector3I.Round(worldPosition));//probs needs rounding
            }

            return calcList;
        }

        private List<Vector3I> ExplosionToGrid(List<Vector3I> calcS1List, Matrix gridMatrix)
        {
            List<Vector3I> calcList = new List<Vector3I>();

            foreach (Vector3I vec in calcS1List)
            {

                //Convert worldPosition into a world direction
                Vector3D worldDirection2 = vec - gridMatrix.Translation; //this is a vector starting at the reference block pointing at your desired position

                //Convert worldDirection into a local direction
                Vector3D bodyPosition = Vector3D.TransformNormal(worldDirection2, MatrixD.Transpose(gridMatrix)); //note that we transpose to go from world -> body

                calcList.Add(Vector3I.Round(bodyPosition)); //You'll proably have to round bodyPosition idk how lmao
            }

            return calcList;

        }

        public override void Execute(IHitInfo hit, BulletBase bullet)
        {
            baseSphere = BoomSphere((int)Math.Round(Radius));

            int j = 1;
            int k = 0;
            int l = 0;

            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            List<IMyCubeGrid> allTheGrids = new List<IMyCubeGrid>();
            foreach (IMyEntity ent in effectedEntities)
            {
                IMyCubeGrid g = ent as IMyCubeGrid;
                if (g != null)
                {
                    allTheGrids.Add(g);
                }
            }

            Matrix epicenterMatrix = Matrix.CreateWorld(bullet.Position, Vector3.Normalize(bullet.Velocity), bullet.Up);
            List<Vector3I> worldSphere = ExplosionToWorld(epicenterMatrix);

            //is this heresy?
            List<List<Vector3I>> vectorListList = new List<List<Vector3I>>(); //this is heresy isn't it?

            foreach (IMyCubeGrid grid in allTheGrids)
            {
                vectorListList.Add(ExplosionToGrid(worldSphere, grid.WorldMatrix));
            }

            int count = 0;
            while (j < baseSphere.Count)
            {
                tempDmgPool = rayDamage;
                while (k < Vector3I.DistanceManhattan(baseSphere[0], baseSphere[j]))
                {
                    while (l < allTheGrids.Count)
                    {
                        MyLog.Default.Info($"Count: {count++}");
                        MyLog.Default.Flush();
                        IMyCubeGrid grid = allTheGrids[l];
                        List<Vector3I> elList = vectorListList[l];
                        RayCrawler(elList[j], elList[0], allTheGrids[l], k);
                        l++;

                    }
                    k++;
                }
                j++;
            }
        }

        private void RayCrawler(Vector3I outer, Vector3I epicenter, IMyCubeGrid grid, int i) //lotta math stuff in here im not sure works in VRage 
        {
            //MyLog.Default.Info($"Pass");
            //MyLog.Default.Flush();
            Vector3I diffVec = outer - epicenter;
            //MyLog.Default.Info($"Pass2");
            //MyLog.Default.Flush();
            int diffManhattan = Vector3I.DistanceManhattan(epicenter, outer);
            MyLog.Default.Info($"diff: {diffVec} length: {diffManhattan}");
            MyLog.Default.Flush();

            long xStep = (long)Math.Floor((float)diffManhattan / (float)diffVec.Z);
            long yStep = (long)Math.Floor((float)diffManhattan / (float)diffVec.Y);
            long zStep = (long)Math.Floor((float)diffManhattan / (float)diffVec.Z);
            MyLog.Default.Info($"{xStep} {yStep} {zStep}  -  {i}");
            MyLog.Default.Flush();

            bool moved = false;

            if (i % Math.Abs(xStep) == 0)
            {
                tempTargetPoint.X += Math.Sign(xStep);
                moved = true;
            }

            if (i % Math.Abs(yStep) == 0)
            {
                tempTargetPoint.Y += Math.Sign(yStep);
                moved = true;
            }

            if (i % Math.Abs(zStep) == 0)
            {
                tempTargetPoint.Z += Math.Sign(zStep);
                moved = true;
            }

            MyLog.Default.Info($"Pass5");
            MyLog.Default.Flush();

            if (moved)
            {
                IMySlimBlock blocker = grid.GetCubeBlock(tempTargetPoint); //you are going to need to do stuff here 
                                                                           //dmg here
                                                                           //dmgPool -= dmgDone;


                MyVisualScriptLogicProvider.AddGPS("", "", Vector3D.Transform(tempTargetPoint, epicenterMatrix), Color.Orange);
                //MyLog.Default.Info($"[{blocker?.CubeGrid?.CustomName}] {blocker?.Position.ToString()}");
                //MyLog.Default.Flush();
            }

        }
    }
}

//private class BlockStatus
//{
//    public float Damage;
//    public float Shielding = 0;
//    public IMySlimBlock Slim;
//    public HashSet<long> Blockers = new HashSet<long>();
//}

//public override void Execute(IHitInfo hit, BulletBase bullet)
//{
//    //MyLog.Default.Info($"Executing Explosion!");
//    Stopwatch watch = new Stopwatch();
//    watch.Start();

//    BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
//    List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
//    //MyLog.Default.Info($"Entities Found: {effectedEntities.Count}");

//    foreach (IMyEntity ent in effectedEntities)
//    {
//        //MyLog.Default.Info(ent.GetType().Name);
//        if (ent is IMyDestroyableObject)
//        {

//        }
//        else if (ent is IMyCubeGrid)
//        {
//            IMyCubeGrid grid = ent as IMyCubeGrid;
//            List<IMySlimBlock> gridBlockList = grid.GetBlocksInsideSphere(ref sphere);
//            float radiusplus = Radius + grid.GridSize;
//            float radiusSq = radiusplus * radiusplus;
//            //int uRadius = (int)Math.Ceiling((Radius / grid.GridSize) + (grid.GridSize / 2));
//            //if (uRadius == 0) { uRadius = 1; }
//            //float uRadiusSquared = uRadius * uRadius;
//            Vector3I epicenter = grid.WorldToGridInteger(hit.Position);

//            //MyLog.Default.Info($"Block Count: {gridBlockList.Count}");
//            //MyLog.Default.Flush();

//            BlockStatus[] processedBlocks = new BlockStatus[gridBlockList.Count];

//            for (int i = 0; i < gridBlockList.Count; i++)
//            {
//                IMySlimBlock slim = gridBlockList[i];


//                BlockStatus currentBlock = new BlockStatus()
//                {
//                    Slim = slim,
//                    Damage = (float)(bullet.Ammo.ProjectileMassDamage * (1 - Vector3D.DistanceSquared(hit.Position, grid.GridIntegerToWorld(slim.Position)) / radiusSq)),
//                };

//                //MyLog.Default.Info($"Radius: {Radius}  -  {(1- Vector3D.DistanceSquared(hit.Position, grid.GridIntegerToWorld(slim.Position)) / radiusSq)}");


//                //MyLog.Default.Info($"start: {epicenter}, target: {slim.Position}, Radius: {Radius} uRadius: {uRadius}, uRadiusSq: {uRadiusSquared}, {(float)Vector3I.DistanceManhattan(epicenter, slim.Position)} , {(float)Vector3D.DistanceSquared(epicenter, slim.Position)} Math {(1f - (float)Vector3D.DistanceSquared(epicenter, slim.Position) / (float)uRadiusSquared)}, {(1f - (float)Vector3I.DistanceManhattan(epicenter, slim.Position) / (float)uRadiusSquared)}");

//                List<Vector3I> intersectingPositions = ExplosionTools.GetBlocksBetweenPoint(epicenter, slim.Position);

//                foreach (Vector3I point in intersectingPositions)
//                {
//                    IMySlimBlock blocker = grid.GetCubeBlock(point);

//                    if (blocker != null)
//                    {
//                        if (blocker.FatBlock == null)
//                        {
//                            currentBlock.Shielding += blocker.Integrity;
//                        }
//                        else if (!currentBlock.Blockers.Contains(blocker.FatBlock.EntityId))
//                        {
//                            currentBlock.Shielding += blocker.Integrity;
//                            currentBlock.Blockers.Add(blocker.FatBlock.EntityId);
//                        }
//                    }
//                }

//                MyLog.Default.Info($"Location: {epicenter}, Location: {slim.Position} Damage: {currentBlock.Damage}, Integrity: {currentBlock.Slim.Integrity}, Shielding: {currentBlock.Shielding}, Intersecting: {intersectingPositions.Count}");

//                processedBlocks[i] = currentBlock;
//            }

//            foreach (BlockStatus block in processedBlocks)
//            {
//                if (block.Damage > block.Shielding)
//                {
//                    block.Slim.DoDamage(block.Shielding - block.Damage, bullet.Ammo.Id.SubtypeId, false, attackerId: bullet.ShooterID);
//                }
//            }
//        }
//    }

//    watch.Stop();
//    MyLog.Default.Info($"Explosion Radius: {Radius} Time: {watch.ElapsedTicks}");
//    MyLog.Default.Flush();

//    NextEffect?.Execute(hit, bullet);
//}