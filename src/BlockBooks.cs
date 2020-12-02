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
        public ICoreAPI Api;
    
        public WorldInteraction[] interactbook;

        public static string
            saveTitle = "booktitle",
            savePageMax = "PageMax";

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
                        // in: game\lang\en.jfson ="Write text",
                        ActionLangCode = "blockhelp-sign-write", 
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
            //TODO: empty book
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
            
            // destroying block:
            base.OnBlockBroken(world,pos,byPlayer,1);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            BlockEntity Entity = world.BlockAccessor.GetBlockEntity(blockPos);

            if (Entity is BlockEntityBooks)
            {
                
                BlockEntityBooks BEBooks = (BlockEntityBooks)Entity;
                ItemStack UniqueBook = byItemStack;
                if ((UniqueBook != null) && (UniqueBook.Attributes.HasAttribute(saveTitle)))
                {

                    // save data to block 
                    //BEBooks.Title = byItemStack.ItemAttributes.GetString("booktitle");
                    BEBooks.PageMax = UniqueBook.Attributes.GetInt(savePageMax);
                    BEBooks.Title = UniqueBook.Attributes.GetString(saveTitle);
                    BEBooks.NamingPages();
                    BEBooks.DeletingText();
                    for (int i = 0; i < BEBooks.PageMax; i++)
                    {
                        BEBooks.arText[i] = UniqueBook.Attributes.GetString(BEBooks.arPageNames[i]);
                    }
                }
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            // showing info on hovering over itemstack in inventory
            

        }

        public override RichTextComponentBase[] GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
        {
            return base.GetHandbookInfo(inSlot, capi, allStacks, openDetailPageFor);
            // Link to handbook on h?
        }

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            // renaming unique books, so title is shown
            BlockEntity beb = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBooks;

            if (beb is BlockEntityBooks)
            {
                BlockEntityBooks BEBooks = (BlockEntityBooks)beb;
                if (BEBooks.Title == "")
                    return base.GetPlacedBlockName(world, pos);
                else
                    return BEBooks.Title;
            }
            return base.GetPlacedBlockName(world, pos);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BlockEntity beb = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBooks;

            if (beb is BlockEntityBooks)
            {
                BlockEntityBooks BEBooks = (BlockEntityBooks) beb;
                ItemStack UniqueBook = new ItemStack(BEBooks.Block,1);
                // to later recall book text, title and pages, save now:
                UniqueBook.Attributes.SetString(saveTitle, BEBooks.Title);
                UniqueBook.Attributes.SetInt(savePageMax, BEBooks.PageMax);
                for (int i = 0; i < BEBooks.PageMax; i++)
                {
                    UniqueBook.Attributes.SetString(BEBooks.arPageNames[i], BEBooks.arText[i]);
                }
                // place in inventory
                return UniqueBook;
            }
            else
            {
                return null;
            }
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
