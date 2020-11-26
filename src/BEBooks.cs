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


namespace books.src
{

    public class BlockEntityBooks : BlockEntity
    {

        public ICoreClientAPI Capi;
        public ICoreServerAPI Sapi;

        //private string[] Pages;
        private string Text = "Enter you text ... ";
        private string Title = "Your title ... ";
        int color;

        // Charcoal default
        int tempColor = ColorUtil.ToRgba(255, 25, 24, 22);
        ItemStack tempStack;
        BooksGui BGui;

        public BlockEntityBooks() : base() { }

        public BlockEntityBooks(BlockPos blockPos) : base()
        {
            this.Pos = blockPos;
        }
        public BlockEntityBooks(BlockPos blockPos, string text) : base()
        {
            this.Pos = blockPos;
            this.Text = text;
            this.Title = "Letter";
        }
        public BlockEntityBooks(BlockPos blockPos, string title, string text) : base()
        {
            this.Pos = blockPos;
            this.Text = text;
            this.Title = title;
        }

        public BlockEntityBooks(ICoreServerAPI sapi) : base()
        {
            this.Sapi = sapi;
        }
        public BlockEntityBooks(ICoreClientAPI capi) : base()
        {
            this.Capi = capi;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Api = api;
        }

        public void OnRightClick(IPlayer byPlayer)
        {

            if (byPlayer?.Entity?.Controls?.Sneak == true)
            {
                //ItemStack tempStack;
                ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if ((hotbarSlot?.Itemstack?.ItemAttributes?["quillink"].Exists == true) || (hotbarSlot?.Itemstack?.ItemAttributes?["pen"].Exists == true))
                {
                    // TODO
                    if (hotbarSlot?.Itemstack?.ItemAttributes?["quillink"]?["color"].Exists == true)
                    {
                        JsonObject jobj = hotbarSlot.Itemstack.ItemAttributes["quillink"]["color"];
                        int r, g, b;
                        r = jobj["red"].AsInt();
                        g = jobj["green"].AsInt();
                        b = jobj["blue"].AsInt();

                        tempColor = ColorUtil.ToRgba(255, r, g, b);
                    }

                    tempStack = hotbarSlot.TakeOut(1);
                    hotbarSlot.MarkDirty();

                    //if (Api.Side != EnumAppSide.Client) return;
                    //Capi = (ICoreClientAPI)Api;
                    //int maxwidth = 120;
                    //int maxLines = 10;

                    // TODO: Add animation, see> ClientAnimator, maybe better animatorbase!
                    // https://apidocs.vintagestory.at/api/Vintagestory.API.Common.AnimationManager.html#Vintagestory_API_Common_AnimationManager_StartAnimation_System_String_
                    //
                    // https://apidocs.vintagestory.at/api/Vintagestory.API.Client.GuiElementCustomDraw.html#Vintagestory_API_Client_GuiElementCustomDraw_OnMouseDownOnElement_Vintagestory_API_Client_ICoreClientAPI_Vintagestory_API_Client_MouseEvent_
                    //
                    // TODO: GUI with bg!

                    // GuiDialogBlockEntityTextInput GUI = new GuiDialogBlockEntityTextInput(Title, Pos, Text, Capi, maxwidth, maxLines);
                    // GUI.TryOpen();

                    if (Api.World is IServerWorldAccessor)
                    {
                        byte[] data;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryWriter writer = new BinaryWriter(ms);
                            writer.Write("BlockEntityTextInput");
                            writer.Write("Book Text");
                            writer.Write(Text);
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
            }
            else {
                // just reading:
                if (Api.Side != EnumAppSide.Client) return;
                Capi = (ICoreClientAPI)Api;
                //int maxwidth = 130;
                //int maxLines = 4;
                if (Text == null) Text = "";
                if (Title == null) Title = "";
                if (BGui == null) BGui = new BooksGui(Title, Text, Capi);
                BGui.ReadGui(Title,Text, Pos, Capi);
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumBookPacketId.SaveText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    Text = reader.ReadString();
                    if (Text == null) Text = "";
                }

                color = tempColor;

                /*((ICoreServerAPI)api).Network.BroadcastBlockEntityPacket(
                    pos.X, pos.Y, pos.Z,
                    (int)EnumSignPacketId.NowText,
                    data
                );*/

                MarkDirty(true);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

                // 85% chance to get back the item
                if (Api.World.Rand.NextDouble() < 0.85)
                {
                    player.InventoryManager.TryGiveItemstack(tempStack);
                }
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
                    string dialogTitle = reader.ReadString();
                    Text = reader.ReadString();
                    if (Text == null) Text = "";
                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
                    if (BGui == null) BGui = new BooksGui(Title, Text, Api as ICoreClientAPI);
                    BGui.WriteGui(Title, Text, Pos, Api as ICoreClientAPI);
                    //dlg.OnTextChanged = DidChangeTextClientSide;
                    BGui.OnCloseCancel = () =>
                    {
                        (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumBookPacketId.CancelEdit, null);
                    };
                    BGui.TryOpen();
                }
            }


            if (packetid == (int)EnumBookPacketId.NowText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    Text = reader.ReadString();
                    if (Text == null) Text = "";
                }
            }
        }
    }
    public enum EnumBookPacketId
    {
        NowText = 1000,
        OpenDialog = 1001,
        SaveText = 1002,
        CancelEdit = 1003

    }
}
