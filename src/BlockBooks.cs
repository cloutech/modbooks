using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Util;


namespace books.src
{

    class BlockBooks : Block
    {

        ICoreAPI Api;
    
        WorldInteraction[] interactbook;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            this.Api = api;

            if (api.Side != EnumAppSide.Client) return;

            interactbook = ObjectCacheUtil.GetOrCreate(api, "BooksBlockInteract", () =>
            {
                List<ItemStack> stacksList = new List<ItemStack>();

                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    if ((collectible.Attributes?["quillink"].Exists == true)
                            ||(collectible.Attributes?["pen"].Exists == true))
                    {
                        stacksList.Add(new ItemStack(collectible));
                    }
                }

                return new WorldInteraction[] { new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-sign-write", // in: game\lang\en.jfson ="Write text",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacksList.ToArray()
                    }
                };
            });
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity Entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            
            if (Entity is BlockEntityBooks)
            {
                BlockEntityBooks BEBooks = (BlockEntityBooks)Entity;
                BEBooks.OnRightClick(byPlayer);
                return true;
             }
             return false;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactbook.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
        /*
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            BEBooks.Pos = blockPos;
        }*/

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            Block block = world
                .BlockAccessor
                .GetBlock(CodeWithParts("ground", "north"));

            //TODO: 
            // Wenn der Block bricht, in den Inv. des Spielers geben mit:
                //  titel, Textinhalt gespeichert!

        }

    }

    public class BlockInkpot : Block
    {

    }
    //public class BlockBooksQuills : Block
    //{ }
    public class ItemQuill : Item
    {

    }
}
