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


namespace books.src
{

    public class BlockEntityBooks : BlockEntity
    {
        public ICoreClientAPI Capi;
        public ICoreServerAPI Sapi;

        private static string
            flag_R = "R",
            flag_W = "W",
            NetworkName = "BlockEntityTextInput";
        private int
            PageMax = 0;
        private static int
            PageLimit = 20;
        private string[]
            arText = new string[PageLimit],
            arPageNames = new string[PageLimit];
        private string Title = "Your title";


        ItemStack tempStack;

        public BlockEntityBooks() : base() { }

        public BlockEntityBooks(BlockPos blockPos) : base()
        {
            DeletingText();
            this.Pos = blockPos;
        }
        
        public BlockEntityBooks(BlockPos blockPos, string title, string[] text) : base()
        {
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

        private void NamingPages()
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

        private void DeletingText()
        {
            for (int i = 0; i < PageMax; i++)
            {
                this.arText[i] = "";
            }
            this.arText[0] = "Your text";
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Api = api;
            NamingPages();
            DeletingText();
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            // TODO: rewrite to only send data on read
            // only always load title info!
            PageMax = tree.GetInt("PageMax",1);
            Title = tree.GetString("title");
            for (int i = 0; i < PageMax; i++)
            {
                arText[i] = tree.GetString(arPageNames[i], "");
            }

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            // TODO: rewrite to only send data on read
            // only always load title info!
            tree.SetInt("PageMax", PageMax);
            tree.SetString("title", Title);
            for (int i = 0; i < PageMax; i++)
            {
                tree.SetString(arPageNames[i], arText[i]);
            }
        }

        public void OnRightClick(IPlayer byPlayer)
        {
            string controlRW = flag_R;

            if (byPlayer?.Entity?.Controls?.Sneak == true)
            {
                ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
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
                    for (int i = 0; i <= PageMax; i++)
                    {
                        writer.Write(arText[i]);
                    }
                    writer.Write(Title);
                    writer.Write(controlRW);
                    data = ms.ToArray();
                }

                        ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            Pos.X, Pos.Y, Pos.Z,
                            (int)EnumBookPacketId.OpenDialog,
                            data
                        );
            }
            // TODO: Add animation, see> ClientAnimator, maybe better animatorbase!
            // https://apidocs.vintagestory.at/api/Vintagestory.API.Common.AnimationManager.html#Vintagestory_API_Common_AnimationManager_StartAnimation_System_String_
            //
            // https://apidocs.vintagestory.at/api/Vintagestory.API.Client.GuiElementCustomDraw.html#Vintagestory_API_Client_GuiElementCustomDraw_OnMouseDownOnElement_Vintagestory_API_Client_ICoreClientAPI_Vintagestory_API_Client_MouseEvent_
            //
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            // TODO: populate BooksNetworkHandler, sloppy:
            if (packetid == (int)EnumBookPacketId.SaveBook)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    PageMax = reader.ReadInt32();
                    for (int i = 0; i <= PageMax; i++)
                    {
                        arText[i] = reader.ReadString();
                    }
                    Title = reader.ReadString();
                }
                NamingPages();
                MarkDirty(true);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();
            }

            if (packetid == (int)EnumBookPacketId.CancelEdit && tempStack != null)
            {
                player.InventoryManager.TryGiveItemstack(tempStack);
                tempStack = null;
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            // TODO: populate BooksNetworkHandler, sloppy:
            if (packetid == (int)EnumBookPacketId.OpenDialog)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    PageMax = reader.ReadInt32();
                    for (int i = 0; i <= PageMax; i++)
                    {
                        arText[i] = reader.ReadString();
                    }
                    Title = reader.ReadString();
                    string controlRW = reader.ReadString();
                     
                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                    if (controlRW.Equals(flag_W))
                    {
                        BooksGui BGuiWrite = new BooksGui(Title, arText, PageMax, Api as ICoreClientAPI, "bookeditor");
                        BGuiWrite.WriteGui(Pos, Api as ICoreClientAPI);
                        BGuiWrite.OnCloseCancel = () =>
                        {
                            (Api as ICoreClientAPI)
                            .Network
                            .SendBlockEntityPacket(
                                Pos.X, Pos.Y, Pos.Z,
                                (int)EnumBookPacketId.CancelEdit,
                                null);
                        };
                        BGuiWrite.TryOpen();
                    }
                    else {
                        BooksGui BGuiRead = new BooksGui(Title, arText, PageMax, Api as ICoreClientAPI, "bookreader");
                        BGuiRead.ReadGui(Pos, Api as ICoreClientAPI);
                        BGuiRead.OnCloseCancel = () =>
                        {
                            (Api as ICoreClientAPI)
                            .Network
                            .SendBlockEntityPacket(
                                Pos.X, Pos.Y, Pos.Z,
                                (int)EnumBookPacketId.CancelEdit,
                                null);
                        };
                        BGuiRead.TryOpen();
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
