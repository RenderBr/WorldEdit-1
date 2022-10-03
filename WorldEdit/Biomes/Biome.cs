﻿using System;
using System.Linq;
using Terraria;

namespace WorldEdit.Biomes
{
    public class Biome
    {
        public virtual int Dirt { get; }
        public virtual int[] Grass { get; } = Array.Empty<int>();
        public virtual int Stone { get; }
        public virtual int Ice { get; }
        public virtual int Clay { get; }
        public virtual int Sand { get; }
        public virtual int HardenedSand { get; }
        public virtual int Sandstone { get; }
        public virtual int Plants { get; }
        public virtual int TallPlants { get; }
        public virtual int Vines { get; }
        public virtual int Thorn { get; }

        public int[] Tiles => new int[]
        {
            Dirt, Stone, Ice, Clay, Sand, HardenedSand, Sandstone, Plants, TallPlants, Vines, Thorn
        }.Concat(Grass).ToArray();

        public virtual ushort DirtWall { get; }
        public virtual ushort StoneWall { get; }
        public virtual ushort HardenedSandWall { get; }
        public virtual ushort SandstoneWall { get; }
        public virtual ushort GrassWall { get; }
        public virtual ushort GrassWallUnsafe { get; }
        public virtual ushort FlowerWall { get; }
        public virtual ushort FlowerWallUnsafe { get; }
        
        public virtual ushort CaveWall1 { get; }
        public virtual ushort CaveWall2 { get; }
        public virtual ushort CaveWall3 { get; }
        public virtual ushort CaveWall4 { get; }
        public virtual ushort DirtWallUnsafe { get; }
        public virtual ushort DirtWallUnsafe1 { get; }
        public virtual ushort DirtWallUnsafe2 { get; }
        public virtual ushort DirtWallUnsafe3 { get; }
        public virtual ushort DirtWallUnsafe4 { get; }
        public virtual ushort CaveWall { get; }

        public ushort[] Walls => new ushort[]
        {
            DirtWall, StoneWall, HardenedSandWall, SandstoneWall,
            GrassWall, GrassWallUnsafe, FlowerWall,
            FlowerWallUnsafe, CaveWall1, CaveWall2, CaveWall3,
            CaveWall4, DirtWallUnsafe, DirtWallUnsafe1,
            DirtWallUnsafe2, DirtWallUnsafe3, DirtWallUnsafe4
        };

        public bool ConvertTile(ITile Tile, Biome ToBiome)
        {
            if (Tile == null) return false;
            bool edited = false;
            if (Tile.active())
            {
                int index = Array.FindIndex(Tiles, t => t == Tile.type);
                if (index >= 0)
                {
                    if (ToBiome.Tiles[index] == -1)
                    {
                        Tile.type = 0;
                        Tile.frameX = -1;
                        Tile.frameY = -1;
                        Tile.active(false);
                    }
                    else { Tile.type = (ushort)ToBiome.Tiles[index]; }
                    edited = true;
                }
            }
            if (Tile.wall > 0)
            {
                int index = Array.FindIndex(Walls, w => w == Tile.wall);
                if (index >= 0 && ToBiome.Walls[index] != 0)
                {
                    Tile.wall = ToBiome.Walls[index];
                    edited = true;
                }
            }
            return edited;
        }
    }
}