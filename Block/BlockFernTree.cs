﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockFernTree : Block, ITreeGenerator
    {
        public Block? trunk;
        public Block? trunkTopYoung;
        public Block? trunkTopMedium;
        public Block? trunkTopOld;
        public Block? foliage;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (Variant["part"] == "trunk") (api as ICoreServerAPI)?.RegisterTreeGenerator(Code, this);

            IBlockAccessor blockAccess = api.World.BlockAccessor;

            trunk ??= blockAccess.GetBlock(CodeWithVariant("part", "trunk"));
            trunkTopYoung ??= blockAccess.GetBlock(CodeWithVariant("part", "trunk-top-young"));
            trunkTopMedium ??= blockAccess.GetBlock(CodeWithVariant("part", "trunk-top-medium"));
            trunkTopOld ??= blockAccess.GetBlock(CodeWithVariant("part", "trunk-top-old"));
            foliage ??= blockAccess.GetBlock(CodeWithVariant("part", "foliage"));
        }

        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            base.OnDecalTesselation(world, decalMesh, pos);
        }

        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);

            if (Variant["part"] == "foliage")
            {
                for (int i = 0; i < sourceMesh.FlagsCount; i++)
                {
                    sourceMesh.Flags[i] = (sourceMesh.Flags[i] & ~VertexFlags.NormalBitMask) | BlockFacing.UP.NormalPackedFlags;
                }
            }
        }


        public string? Type()
        {
            return Variant["type"];
        }


        public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treeGenParams, IRandom rand)
        {
            float f = treeGenParams.otherBlockChance == 0 ? 1 + (float)rand.NextDouble() * 2.5f : 1.5f + (float)rand.NextDouble() * 4;
            int quantity = GameMath.RoundRandom(rand, f);

            while (quantity-- > 0)
            {
                GrowOneFern(blockAccessor, pos.UpCopy(), treeGenParams.size, treeGenParams.vinesGrowthChance, rand);

                // Potentially grow another one nearby
                pos.X += rand.NextInt(8) - 4;
                pos.Z += rand.NextInt(8) - 4;

                // Test up to 2 blocks up and down.
                bool foundSuitableBlock = false;
                for (int y = 2; y >= -2; y--)
                {
                    Block block = blockAccessor.GetBlockAbove(pos, y);
                    if (block.Fertility > 0 && !blockAccessor.GetBlockAbove(pos, y + 1, BlockLayersAccess.Fluid).IsLiquid())
                    {
                        pos.Y = pos.Y + y;
                        foundSuitableBlock = true;
                        break;
                    }
                }
                if (!foundSuitableBlock) break;
            }


        }

        private void GrowOneFern(IBlockAccessor blockAccessor, BlockPos upos, float sizeModifier, float vineGrowthChance, IRandom rand)
        {
            int height = GameMath.Clamp((int)(sizeModifier * (2 + rand.NextInt(6))), 2, 6);

            Block? trunkTop = height > 2 ? trunkTopOld : (height != 1 ? trunkTopMedium : trunkTopYoung);
            if (height == 1) trunkTop = trunkTopYoung;

            for (int i = 0; i < height; i++)
            {
                Block? toplaceblock = (i == height - 2) ? trunkTop : (i == height - 1) ? foliage : trunk;

                if (toplaceblock != null && !blockAccessor.GetBlockAbove(upos, i).IsReplacableBy(toplaceblock)) return;
            }

            for (int i = 0; i < height; i++)
            {
                Block? toplaceblock = (i == height - 2) ? trunkTop : (i == height - 1) ? foliage : trunk;

                if (toplaceblock != null) blockAccessor.SetBlock(toplaceblock.BlockId, upos);
                upos.Up();
            }
        }

    }
}
