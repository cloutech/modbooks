using System;
using System.Collections.Generic;
using System.Text;
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
    class BlockPaper : Block
    {
        public ICoreAPI Api;

        public WorldInteraction[] interactbook;

        public static bool
            isPaper = true;

        public static string
            IDInteract = "BooksBlockInteract",
            defTitle = "books: books - north",
            // required Items:
            reqItem1 = "quillink",
            reqItem2 = "pen",
            // TreeAttribute names:
            saveTitle = "booktitle",
            savePageMax = "PageMax",
            saveIsUnique = "isunique",
            saveAuthor = "author",
            defaultAuthor = "Unknown",
            // Keyboard hotkey:
            _HotKeyWrite = "sneak",
            _HotKeyClose = "sprint",
            // Action Language Code mouseover:
            ALCHelpClose = "books:books-mouse-over-help-close",
            ALCHelpWrite = "books:books-mouse-over-help-write",
            ALCHelpRead = "books:books-mouse-over-help-read",
            // On mouse hover itemstack:
            descr = "Title: ",
            author = "Author: ";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.Api = api;

            if (api.Side != EnumAppSide.Client) return;

            interactbook = ObjectCacheUtil.GetOrCreate(api, IDInteract, () =>
            {
                List<ItemStack> stacksList = new List<ItemStack>();
                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    if ((collectible.Attributes?[reqItem1].Exists == true)
                            || (collectible.Attributes?[reqItem2].Exists == true))
                    {
                        stacksList.Add(new ItemStack(collectible));
                    }
                }
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = ALCHelpRead,
                        MouseButton = EnumMouseButton.Right
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = ALCHelpWrite,
                        HotKeyCode = _HotKeyWrite,
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
                BEBooks.OnRightClick(byPlayer, isPaper);
                return true;
            }
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactbook.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos blockPos, IPlayer byPlayer, float dropQuantityMultiplier = 0)
        {
            BlockEntity beb = world.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityBooks;

            if (beb is BlockEntityBooks)
            {
                BlockEntityBooks BEBooks = (BlockEntityBooks)beb;

                if (BEBooks.Unique)
                {
                    ItemStack UniqueBook = new ItemStack(api.World.BlockAccessor.GetBlock(blockPos));
                    TreeAttribute BookTree = new TreeAttribute();
                    BookTree.SetString(saveTitle, BEBooks.Title);
                    BookTree.SetString(saveAuthor, defaultAuthor);
                    BookTree.SetInt(savePageMax, BEBooks.PageMax);
                    BookTree.SetBool(saveIsUnique, BEBooks.Unique);
                    for (int i = 0; i < BEBooks.PageMax; i++)
                    {
                        BookTree.SetString(BEBooks.arPageNames[i], BEBooks.arText[i]);
                    }
                    UniqueBook.Attributes = BookTree;
                    UniqueBook.ResolveBlockOrItem(world);
                    api.World.SpawnItemEntity(UniqueBook, blockPos.ToVec3d());
                    api.World.BlockAccessor.RemoveBlockEntity(blockPos);
                    api.World.BlockAccessor.SetBlock(0, blockPos);
                    return;
                }
            }
            base.OnBlockBroken(world, blockPos, byPlayer);
        }


        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (world.Api is ICoreServerAPI)
            {
                BlockEntity be = world.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityBooks;
                if (be is BlockEntityBooks)
                {
                    BlockEntityBooks BEBooks;
                    BEBooks = (BlockEntityBooks)be;
                    BEBooks.PageMax = byItemStack.Attributes.GetInt(savePageMax, 1);
                    BEBooks.Title = byItemStack.Attributes.GetString(saveTitle, "");
                    BEBooks.Author = byItemStack.Attributes.GetString(saveAuthor, "Unknown");
                    BEBooks.Unique = byItemStack.Attributes.GetBool(saveIsUnique, false);
                    BEBooks.DeletingText();
                    BEBooks.NamingPages();
                    for (int i = 0; i < BEBooks.PageMax; i++)
                    {
                        BEBooks.arText[i] = byItemStack.Attributes.GetString(BEBooks.arPageNames[i], "");
                    }
                    world.BlockAccessor.MarkBlockDirty(blockPos);
                    world.BlockAccessor.MarkBlockEntityDirty(blockPos);
                }
            }
        }


        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            // TODO: displaying author? 
            // Structure of dsc = 
            // { Material: Wood
            // Id: 4822
            // Code: books: books - north
            // Burn temperature: 600°C
            // Burn duration: 12s
            // }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot.Itemstack.Attributes.GetBool(saveIsUnique))
            {
                if (inSlot.Itemstack.Attributes.HasAttribute(saveTitle))
                {
                    int len;
                    string
                        temp = "",
                        title = inSlot.Itemstack.Attributes.GetString(saveTitle);
                    dsc.Replace("Wood", "Paper");
                    temp = string.Concat(descr, title, "\n");
                    dsc.Insert(0, temp);
                    len = temp.Length;
                    if (inSlot.Itemstack.Attributes.HasAttribute(saveAuthor))
                    {
                        temp = string.Concat(author, inSlot.Itemstack.Attributes.GetString(saveAuthor), "\n", "\n");
                        dsc.Insert(len, temp);
                    }
                }
            }
        }


        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            // renaming unique books, so title is shown for easier handling
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
    }
}
