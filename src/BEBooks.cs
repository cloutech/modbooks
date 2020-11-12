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

        private string Text = "";
        private string Title = "";
        //int color;
        // Charcoal default
        public int tempColor = ColorUtil.ToRgba(255, 25, 24, 22);

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

        public BlockEntityBooks(ICoreServerAPI sapi):base()
        {
            this.Sapi = sapi;
        }
        public BlockEntityBooks(ICoreClientAPI capi):base()
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
                ItemStack tempStack;
                ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if ((hotbarSlot?.Itemstack?.ItemAttributes?["quillink"].Exists == true) || (hotbarSlot?.Itemstack?.ItemAttributes?["pen"].Exists == true))
                {
                    if (hotbarSlot?.Itemstack?.ItemAttributes?["quillink"]?["color"].Exists == true)
                    {
                        JsonObject jobj = hotbarSlot.Itemstack.ItemAttributes["quillink"]["color"];     // TODO 
                        int r, g, b;
                        r = jobj["red"].AsInt();
                        g = jobj["green"].AsInt();
                        b = jobj["blue"].AsInt();

                        tempColor = ColorUtil.ToRgba(255, r, g, b);
                    }
                    
                    tempStack = hotbarSlot.TakeOut(1);
                    hotbarSlot.MarkDirty();
                     
                    if (Api.Side != EnumAppSide.Client) return;
                    Capi = (ICoreClientAPI)Api;
                    int maxwidth = 120;
                    int maxLines = 10;
                    
                    // TODO: GUI schreiben!
                    //GuiDialogBlockEntityTextInput GUI = new GuiDialogBlockEntityTextInput( Title, Pos, Text, Capi, maxwidth, maxLines);
                }
            }
        }    
    }
}
