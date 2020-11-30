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

        static string 
            flag_R = "R",
            flag_W = "W";

        private string[]
            arText = {"Your text","","","","","","","","",""}, //10, for debug
            arPageNames = {"page1","page2","page3", "page4","page5","page6","page7","page8","page9", "page10"};
        private string Title = "Your title";
        private int
            PageCurrent = 0,
            PageMax = 9;

        ItemStack tempStack;

        public BlockEntityBooks() : base() { }

        public BlockEntityBooks(BlockPos blockPos) : base()
        {
            SetText();

            this.Pos = blockPos;
        }
        
        public BlockEntityBooks(BlockPos blockPos, string title, string[] text) : base()
        {
            SetText();
            this.Pos = blockPos;
            this.arText = text;
            this.Title = title;
        }

        public BlockEntityBooks(ICoreServerAPI sapi) : base()
        {
            SetText();

            this.Sapi = sapi;
        }
        public BlockEntityBooks(ICoreClientAPI capi) : base()
        {
            SetText();

            this.Capi = capi;
        }

        private void SetText()
        {
            for (int i = 0; i < PageMax; i++)
            {
                this.arText[i] = "";
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Api = api;
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            for (int i = 0; i < PageMax; i++)
            {
                arText[i] = tree.GetString(arPageNames[i], "");
            }
            Title = tree.GetString("title", "");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            for (int i = 0; i < PageMax; i++)
            {
                tree.SetString(arPageNames[i], arText[i]);
            }
            tree.SetString("title", Title);
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
                    writer.Write("BlockEntityTextInput");
                    writer.Write(Title);
                    //TODO
                    writer.Write(arText[PageCurrent]);
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

            /*
            else {
                // just reading on RightClick:
                if (Api.Side != EnumAppSide.Client) return;
                Capi = (ICoreClientAPI)Api;
                if (Text == null) Text = "";
                if (Title == null) Title = "";

                BooksGui BGui = new BooksGui(Title, Text, Api as ICoreClientAPI);
                BGui.ReadGui(Title, Text, Pos, Api as ICoreClientAPI);
                BGui.TryOpen();

            } */
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumBookPacketId.SaveBook)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    // TODO:
                    arText[PageCurrent] = reader.ReadString();
                    Title = reader.ReadString();
                }
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
            if (packetid == (int)EnumBookPacketId.OpenDialog)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    Title = reader.ReadString();
                    //TODO fix array
                    arText[PageCurrent] = reader.ReadString();
                    string controlRW = reader.ReadString(); 
                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                    BooksGui BGui = new BooksGui(Title, arText, Api as ICoreClientAPI);
                    if(controlRW.Equals(flag_W))
                        BGui.WriteGui(Title, arText, Pos, Api as ICoreClientAPI);
                    else //TODO fix array
                        BGui.ReadGui(Title, arText[PageCurrent], Pos, Api as ICoreClientAPI);

                    BGui.OnCloseCancel = () =>
                    {
                        (Api as ICoreClientAPI)
                        .Network
                        .SendBlockEntityPacket(
                            Pos.X, Pos.Y, Pos.Z,
                            (int)EnumBookPacketId.CancelEdit,
                            null);
                    };
                    BGui.TryOpen();
                }
            }


            if (packetid == (int)EnumBookPacketId.NowText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    //TODO fix array networking
                    arText[PageCurrent] = reader.ReadString();
                }
            }
        }
    }
    public enum EnumBookPacketId
    {
        NowText = 1000,
        OpenDialog = 1001,
        SaveBook = 1002,
        CancelEdit = 1003
    }
}
