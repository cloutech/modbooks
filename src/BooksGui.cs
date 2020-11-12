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
    class BooksGui : GuiDialogGeneric
    {

        ICoreClientAPI Capi;

        string dialogTitle = "";
        public string text = "";

         public BooksGui (ICoreClientAPI capi) : base("", capi)
        {
            this.Capi = capi;
        }

        public BooksGui (string dialogTitle, ICoreClientAPI capi) : base (dialogTitle, capi)
        {
            string DialogTitle = dialogTitle;
            this.Capi = capi;

        }

        public bool CreateBookGui(string dialogTitle, BlockPos blockEntityPos, string text, ICoreClientAPI capi)
        {
            int maxwidth = 130;
            int maxLines = 4;
            GuiDialogBlockEntityTextInput GuiTextInput = new GuiDialogBlockEntityTextInput(dialogTitle, blockEntityPos, text,  capi, maxwidth, maxLines);

            return true;
        }
    }
}
