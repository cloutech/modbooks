using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace books.src
{
    class BooksGui : GuiDialogGeneric
    {
        //List<BooksChapters> chapteritems = new List<BooksChapters>();
        private string Title = "";
        private string Text = "";
        //private int Page = 0;
        ICoreClientAPI Capi;

        double textareaFixedY;
        BlockPos BEPos;
        public Action<string> OnTextChanged;
        public Action OnCloseCancel;

        bool didSave;
        public BooksGui(string title, string text, ICoreClientAPI capi) : base(title, capi)
        {
            this.Title = DialogTitle;
            this.Text = text;
            this.Capi = capi;
        }
        public BooksGui(string title, string text, BlockPos BlockEntityPosition, ICoreClientAPI capi) : base(title, capi)
        {
            this.Title = DialogTitle;
            this.Text = text;
            this.BEPos = BlockEntityPosition;
            this.Capi = capi;
        }


        public void WriteGui(string title, string text, BlockPos Pos, ICoreClientAPI Capi)
        {
            int maxLines = 12;
            int maxWidth = 100;
            ElementBounds textAreaBounds = ElementBounds.Fixed(0, 0, 300, 150);
            textareaFixedY = textAreaBounds.fixedY;

            // Clipping bounds for textarea
            ElementBounds clippingBounds = textAreaBounds.ForkBoundingParent().WithFixedPosition(0, 30);

            //ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(textAreaBounds.fixedWidth + 3).WithFixedWidth(20);
            ElementBounds AddPageButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 2).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10, 2);
            ElementBounds SubPageButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 2).WithAlignment(EnumDialogArea.LeftMiddle).WithFixedPadding(10, 2);
            ElementBounds CancelButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 5).WithAlignment(EnumDialogArea.RightMiddle).WithFixedPadding(10, 2);
            ElementBounds SaveButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(clippingBounds, 2 * 5).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10, 2);


            // 2. Around all that is 10 pixel padding
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(clippingBounds, CancelButtonBounds, SaveButtonBounds, AddPageButtonBounds, SubPageButtonBounds); //textAreaBounds, , scrollbarBounds

            // 3. Finally Dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);


            SingleComposer = capi.Gui
                .CreateCompo("blockentitytexteditordialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .BeginClip(clippingBounds)
                    .AddTextArea(textAreaBounds, OnTextAreaChanged, CairoFont.TextInput().WithFontSize(20), "text")
                    .EndClip()
                    //.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
                    .AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, CancelButtonBounds)
                    .AddSmallButton(Lang.Get("-"), OnButtonSub, SubPageButtonBounds)
                    .AddSmallButton(Lang.Get("+"), OnButtonAdd, SubPageButtonBounds)
                    .AddSmallButton(Lang.Get("Save"), OnButtonSave, SaveButtonBounds)
                .EndChildElements()
                .Compose()
            ;

            SingleComposer.GetTextArea("text").SetMaxLines(maxLines);
            SingleComposer.GetTextArea("text").SetMaxWidth((int)(maxWidth * RuntimeEnv.GUIScale));

            SingleComposer.GetScrollbar("scrollbar").SetHeights(
                (float)textAreaBounds.fixedHeight, (float)textAreaBounds.fixedHeight
            );

            if (Text.Length > 0)
            {
                SingleComposer.GetTextArea("text").SetValue(Text);
            }
        }

        public bool OnButtonSub()
        {
            return true;
        }
        public bool OnButtonAdd()
        {
            return true;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            SingleComposer.FocusElement(SingleComposer.GetTextArea("text").TabIndex);
        }

        private void OnTextAreaChanged(string value)
        {
            GuiElementTextArea textArea = SingleComposer.GetTextArea("text");
            SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.fixedHeight);

            OnTextChanged?.Invoke(textArea.GetText());
        }

        private void OnNewScrollbarvalue(float value)
        {
            GuiElementTextArea textArea = SingleComposer.GetTextArea("text");

            textArea.Bounds.fixedY = 3 + textareaFixedY - value;
            textArea.Bounds.CalcWorldBounds();
        }


        private void OnTitleBarClose()
        {
            OnButtonCancel();
        }

        private bool OnButtonSave()
        {
            GuiElementTextArea textArea = SingleComposer.GetTextArea("text");
            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(textArea.GetText());
                data = ms.ToArray();
            }

            capi.Network.SendBlockEntityPacket(BEPos.X, BEPos.Y, BEPos.Z, (int)EnumBookPacketId.SaveText, data);
            didSave = true;
            TryClose();
            return true;
        }

        private bool OnButtonCancel()
        {
            TryClose();
            return true;
        }

        public override void OnGuiClosed()
        {
            if (!didSave) OnCloseCancel?.Invoke();
            base.OnGuiClosed();
        }


        public void ReadGui()
        {

        }



    }
}
