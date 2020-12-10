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

// TODO : Privileges?
// TODO : Networkhandler?
// TODO : save authorID?

namespace books.src
{

    public class BlockEntityBooks : BlockEntity
    {
        public ICoreClientAPI Capi;
        public ICoreServerAPI Sapi;

        public int
            // current page/pagemax <= pagelimit
            PageMax = 1;
        private static int
            PageLimit = 20;
        private static string
            // ID/dialog keys:
            IDDialogBookEditor = "bookeditor",
            IDDialogBookReader = "bookreader",
            // control falgs for read write gui
            flag_R = "R",
            flag_W = "W",
            NetworkName = "BlockEntityTextInput";
        public string[]
            arText = new string[PageLimit],
            arPageNames = new string[PageLimit];
        public string
            Title = "",
            Author = "";

        public bool 
            isPaper = false,
            Unique;

        public ItemStack tempStack;
        private BooksAnimationHandler BookAnim;

        public BlockEntityBooks() : base() { }


        public BlockEntityBooks(BlockPos blockPos, bool isPaper) : base()
        {
            this.isPaper = isPaper;
            DeletingText();
            this.Pos = blockPos;
        }
        
        public BlockEntityBooks(BlockPos blockPos, bool isPaper, string title, string[] text) : base()
        {
            this.isPaper = isPaper;
            DeletingText();
            this.Pos = blockPos;
            this.arText = text;
            this.Title = title;
        }

        public BlockEntityBooks(ICoreServerAPI sapi) : base()
        {
            DeletingText();
            this.Sapi = sapi;
        }
        public BlockEntityBooks(ICoreClientAPI capi) : base()
        {
            DeletingText();
            this.Capi = capi;
        }

        public void NamingPages()
        {
            // naming for saving in tree attributes, e.g. page1
            string
                updatedPageName = "page",
                temp_numbering = "";

            for (int i = 1; i <= PageLimit; i++)
            {
                temp_numbering = i.ToString();
                arPageNames[i-1] = string.Concat(
                    updatedPageName,
                    temp_numbering
                    );
            }
       }

        public void DeletingText()
        {
            for (int i = 0; i < PageLimit; i++)
            {
                this.arText[i] = "";
            }
          
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if ((Api is ICoreClientAPI) && (!isPaper))
                return BookAnim.HideDrawModel();
            else
                return false;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Api = api;

            if ((api is ICoreClientAPI)&& (!isPaper))
                BookAnim = new BooksAnimationHandler(api as ICoreClientAPI, this);

            if (arPageNames == null)
            {
                NamingPages();
            }
            if (!Unique)
                DeletingText();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            // TODO: rewrite to only send data on read
            // only always load title info!
            Unique = tree.GetBool("unique", false);
            PageMax = tree.GetInt("PageMax", 1);
            Title = tree.GetString("title", "");
            if (arPageNames[0] == null)
            { 
                NamingPages();
            }
            if (!Unique)
                DeletingText();
            for (int i = 0; i < PageMax; i++)
            {
                arText[i] = tree.GetString(arPageNames[i], "");
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("unique", Unique);
            tree.SetInt("PageMax", PageMax);
            tree.SetString("title", Title);
            if (arPageNames[0] == null)
            {
                NamingPages();
            }
            if (!Unique)
                DeletingText();
            // TODO: rewrite to only send data on read
            // only always load title and maxpage number info!
            for (int i = 0; i < PageMax; i++)
            {
                tree.SetString(arPageNames[i], arText[i]);
            }
        }

        public override void OnBlockBroken()
        {
            // unregister renderer?
            if ((Api is ICoreClientAPI) && (!isPaper))
                BookAnim.Dispose();
            // keep data
            // base.OnBlockBroken(); 
        }
        

        public void OnRightClick(IPlayer byPlayer, bool isPaper)
        {
            string controlRW = flag_R;

            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if(isPaper)
                this.isPaper = isPaper;

            if (arText[0]== null)
                DeletingText();

            if (byPlayer?.Entity?.Controls?.Sprint == true)
            {
                if ((Api is ICoreClientAPI) && (!isPaper))
                    BookAnim.Close(Api);
            }

            if (byPlayer?.Entity?.Controls?.Sneak == true)
            {
                if ((hotbarSlot?.Itemstack?.ItemAttributes?["quillink"].Exists == true)
                        || (hotbarSlot?.Itemstack?.ItemAttributes?["pen"].Exists == true))
                {
                    tempStack = hotbarSlot.TakeOut(1);
                    hotbarSlot.MarkDirty();
                    controlRW = flag_W;
                }
            }

            if (Api.World is IServerWorldAccessor)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(NetworkName);
                    writer.Write(PageMax);
                    for (int i = 0; i < PageMax; i++)
                    {
                        writer.Write(arText[i]);
                    }
                    writer.Write(Title);
                    writer.Write(controlRW);
                    writer.Write(Unique);

                    data = ms.ToArray();
                }

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBookPacketId.OpenDialog,
                    data
                );
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            // TODO: populate BooksNetworkHandler:
            if (packetid == (int)EnumBookPacketId.SaveBook)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    PageMax = reader.ReadInt32();
                    for (int i = 0; i < PageMax; i++)
                    {
                        arText[i] = reader.ReadString();
                    }
                    Title = reader.ReadString();
                    Unique = reader.ReadBoolean();
                }
                NamingPages();
                MarkDirty(true);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();
            }

            if (packetid == (int)EnumBookPacketId.CancelEdit && tempStack != null)
            {
                player.InventoryManager.TryGiveItemstack(tempStack);
            }
            tempStack = null;

        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            // TODO: populate BooksNetworkHandler, sloppy for now:
            if (packetid == (int)EnumBookPacketId.OpenDialog)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    PageMax = reader.ReadInt32();
                    for (int i = 0; i < PageMax; i++)
                    {
                        arText[i] = reader.ReadString();
                    }
                    Title = reader.ReadString();
                    string controlRW = reader.ReadString();
                    bool unique = reader.ReadBoolean();

                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;


                    if (controlRW.Equals(flag_W))
                    {
                        BooksGui BGuiWrite = new BooksGui(isPaper, unique, Title, arText, PageMax, Api as ICoreClientAPI, IDDialogBookEditor);
                        BGuiWrite.WriteGui(Pos, Api as ICoreClientAPI);
                        BGuiWrite.OnCloseCancel = () =>
                        {
                            if ((Api is ICoreClientAPI) && (!isPaper))
                                BookAnim.Close();
                            (Api as ICoreClientAPI)
                            .Network
                            .SendBlockEntityPacket(
                                Pos.X, Pos.Y, Pos.Z,
                                (int)EnumBookPacketId.CancelEdit,
                                null);
                        };
                        BGuiWrite?.TryOpen();
                    }
                    else {
                        BooksGui BGuiRead = new BooksGui(isPaper, unique, Title, arText, PageMax, Api as ICoreClientAPI, IDDialogBookReader);
                        BGuiRead.ReadGui(Pos, Api as ICoreClientAPI);
                        BGuiRead.OnCloseCancel = () =>
                        {
                            if ((Api is ICoreClientAPI) && (!isPaper))
                                BookAnim.Close();
                            (Api as ICoreClientAPI)
                            .Network
                            .SendBlockEntityPacket(
                                Pos.X, Pos.Y, Pos.Z,
                                (int)EnumBookPacketId.CancelEdit,
                                null);
                        };
                        BGuiRead?.TryOpen();
                    }
                    if ((Api is ICoreClientAPI) && (!isPaper))
                    {
                        BookAnim.Open(Api);
                    }
                }
            }
        }
    }
    public enum EnumBookPacketId
    {
        OpenDialog = 5301,
        SaveBook = 5302,
        CancelEdit = 5303
    }
}
