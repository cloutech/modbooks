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
    class BooksHandler
    {
        ICoreClientAPI Capi;
        ICoreServerAPI Sapi;

        BooksHandler()
        {
            
        }


        public void SendToClient(ICoreClientAPI capi)
        {
            Capi = capi;
        } 


    }
}
