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

    public class BBooks : ModSystem
    {
        // Client:
        public ICoreClientAPI Capi { get; private set; }
        public IClientNetworkChannel CChannel { get; private set; }


        // Server:
        public ICoreServerAPI Sapi { get; private set; }
        public IServerNetworkChannel SChannel { get; private set; }



        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        public override void Start(ICoreAPI api) // starts client and server side!
        {
            base.Start(api);
            
            api.RegisterBlockClass("BlockBooks", typeof(BlockBooks));                        
            api.RegisterBlockEntityClass("BlockEntityBooks", typeof(BlockEntityBooks)); 
            api.RegisterBlockClass("inkpot", typeof(BlockInkpot));
            api.RegisterItemClass("itemquill", typeof(ItemQuill));

            // TODO : needs new shape
            //api.RegisterBlockClass("booksquills", typeof(BlockBooksQuills)); 

            //BlockBooks BBooks = new BlockBooks();
            // TODO : NetworkHandler NetworkHandler = new NetworkHandler();

        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            base.StartClientSide(capi);

            ICoreClientAPI Capi = capi;

        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            base.StartServerSide(sapi);

            ICoreServerAPI Sapi = sapi;

        }

    }

}
