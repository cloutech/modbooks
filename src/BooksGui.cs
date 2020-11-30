using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace books.src
{
    class BooksGui : GuiDialogGeneric
    {
        ICoreClientAPI Capi;

        private string
            Title = "Your title",
            CurrentPageNumbering = "1/1",
            CompNameRead = "blockentitytextreaddialog",
            CompNameEdit = "blockentitytexteditordialog",
            EditTitle = "Book Editor",
            IDTextArea = "text",
            IDRichtextArea = "page",
            IDTitleInput = "title",
            IDPageArea = "page-numbering",
            _bCancel = "Cancel",
            _bSave = "Save",
            _bClose = "Close",
            _bSub = "-",
            _bAdd = "+",
            _bNextPage = ">>",
            _bPrevPage = "<<";
        private int PageCurrent = 0;
        public int PageMax { get; private set; }

        private static int
            MaxTitleWidth = 240,
            MaxLines = 18,
            MaxWidth = 580,
            // PageLimit, how many pages players can have in a book
            PageLimit = 50,
            PageNumberingFont = 18,
            PageNumberingHeight = 20,
            PageNumberingWidth = 50,
            SaveButtonOffsetX = 80,
            TitleX = 20,
            TitleY = 40,
            TitleWidth = 250,
            TitleHeight = 20,
            TitleFont = 18,
            TextFont = 18,
            TextX = 0,
            TextY = 20,
            WindowWidth = 600,
            WindowHeight = 400;

        private string[] Text = new string[PageLimit];

        BlockPos BEPos;
        public Action<string> OnTextChanged;
        public Action OnCloseCancel;

        bool didSave;

        public BooksGui(string title, string[] text, int pagemax, ICoreClientAPI capi) : base(title, capi)
        {
            Capi = capi;
            Title = title;
            PageMax = pagemax;
            //DeletingText();
            text.CopyTo(Text, 0);
        }

        public BooksGui(string title, string[] text, int pagemax, BlockPos BlockEntityPosition, ICoreClientAPI capi) : base(title, capi)
        {
            Capi = capi;
            BEPos = BlockEntityPosition;
            Title = title;
            PageMax = pagemax;
            //DeletingText();
            text.CopyTo(Text, 0);
        }

        private void DeletingText()
        {
            for (int i = 0; i < PageLimit; i++)
            {
                Text[i] = "";
            }
            Text[0] = "Your text";
        }

        private void UpdatingCurrentPageNumbering()
        {
            int temp_page = 0;
            string
                updatedCurrentPageNumbering = "",
                currentPage = "",
                dividerLayout = "/",
                lastPage = "";

            // Display purpose only: 1 to PageMax+1 instead of 0 to PageMax, e.g. 0/9 is 1/10
            temp_page = PageCurrent + 1;
            currentPage = temp_page.ToString();
            temp_page = PageMax + 1;
            lastPage = temp_page.ToString();

            updatedCurrentPageNumbering = string.Concat(
                currentPage,
                dividerLayout,
                lastPage
                );

            CurrentPageNumbering = updatedCurrentPageNumbering.ToString();

            SingleComposer
                .GetDynamicText(IDPageArea)
                .SetNewText(CurrentPageNumbering, false, true, false);
        }

        private bool SavingInputTemporary()
        {
            Title = SingleComposer
                .GetTextInput(IDTitleInput)
                .GetText();
            Text[PageCurrent] = SingleComposer
                .GetTextArea(IDTextArea)
                .GetText();
            return true;
        }

        public void WriteGui(BlockPos pos, ICoreClientAPI Capi)
        {
            ElementBounds
                TitleAreaBounds = ElementBounds
                    .Fixed(TitleX, TitleY, TitleWidth, TitleHeight),
                TextAreaBounds = ElementBounds
                    .Fixed(TextX, TextY, WindowWidth, WindowHeight),
                ClippingBounds = TextAreaBounds
                    .ForkBoundingParent()
                    .WithFixedPosition(0, 50),
                AddPageButtonBounds = ElementBounds
                    .FixedSize(0, 0)
                    .FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(((WindowWidth/2)+10), 0)
                    .WithFixedPadding(3, 2),
                SubPageButtonBounds = ElementBounds
                    .FixedSize(0, 0)
                    .FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(((WindowWidth/2)-10), 0)
                    .WithFixedPadding(4, 2),
                CancelButtonBounds = ElementBounds
                    .FixedSize(0, 0).FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(0, 0)
                    .WithFixedPadding(10, 2),
                SaveButtonBounds = ElementBounds
                    .FixedSize(0, 0).FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(SaveButtonOffsetX, 0)
                    .WithFixedPadding(10, 2),
                NextPageButtonBounds = ElementBounds
                    .FixedSize(0, 0).FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(((WindowWidth / 2) + 29), 0)
                    .WithFixedPadding(3, 2),
                PrevPageButtonBounds = ElementBounds
                    .FixedSize(0, 0).FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(((WindowWidth / 2) - 40), 0)
                    .WithFixedPadding(4, 2),
                PageNumberingAreaBounds = ElementBounds
                    .FixedSize(PageNumberingWidth, PageNumberingHeight)
                    .FixedUnder(ClippingBounds, 2 * 5)
                    .WithAlignment(EnumDialogArea.RightFixed),
                bgBounds = ElementBounds
                    .Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding),
                dialogBounds = ElementStdBounds
                    .AutosizedMainDialog
                    .WithAlignment(EnumDialogArea.RightMiddle)
                    .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            double
                TextareaFixedY = TextAreaBounds.fixedY,
                TitleAreaFixedY = TitleAreaBounds.fixedY;

            this.BEPos = pos;

            bgBounds.BothSizing = ElementSizing.FitToChildren; 
            bgBounds.WithChildren(
                ClippingBounds,
                CancelButtonBounds,
                SaveButtonBounds,
                SubPageButtonBounds,
                AddPageButtonBounds,
                NextPageButtonBounds,
                PrevPageButtonBounds,
                PageNumberingAreaBounds
            ); 

            SingleComposer = capi.Gui
                .CreateCompo(CompNameEdit, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(EditTitle, OnTitleBarClose)
                .AddTextInput(
                    TitleAreaBounds, 
                    OnTitleAreaChanged, 
                    CairoFont.TextInput().WithFontSize(TitleFont), 
                    IDTitleInput)
                .BeginChildElements(bgBounds)
                    .BeginClip(ClippingBounds)
                        .AddTextArea(
                            TextAreaBounds, 
                            OnTextAreaChanged, 
                            CairoFont.TextInput().WithFontSize(TextFont), 
                            IDTextArea)
                    .EndClip()
                    .AddSmallButton(Lang.Get(_bCancel), OnButtonCancel, CancelButtonBounds)
                    .AddSmallButton(Lang.Get(_bSave), OnButtonSave, SaveButtonBounds)
                    .AddSmallButton(Lang.Get(_bSub), OnButtonSub, SubPageButtonBounds)
                    .AddSmallButton(Lang.Get(_bAdd), OnButtonAdd, AddPageButtonBounds)
                    .AddSmallButton(Lang.Get(_bNextPage), OnButtonNextPage, NextPageButtonBounds)
                    .AddSmallButton(Lang.Get(_bPrevPage), OnButtonPrevPage, PrevPageButtonBounds)
                    .AddDynamicText(
                        CurrentPageNumbering,
                        CairoFont.TextInput().WithFontSize(PageNumberingFont),
                        EnumTextOrientation.Center,
                        PageNumberingAreaBounds,
                        IDPageArea)
                .EndChildElements()
                .Compose()
            ;

            SingleComposer
                .GetTextArea(IDTextArea)
                .SetMaxLines(MaxLines);
            SingleComposer
                .GetTextArea(IDTextArea)
                .SetMaxWidth((int)(MaxWidth * RuntimeEnv.GUIScale));
            SingleComposer
                .GetTextInput(IDTitleInput)
                .SetMaxWidth(MaxTitleWidth);

            if (Text.Length > 0){
                SingleComposer
                    .GetTextArea(IDTextArea)
                    .SetValue(Text[PageCurrent]);
            }
            if (Title.Length > 0){
                SingleComposer
                    .GetTextInput(IDTitleInput)
                    .SetValue(Title);
            }
            if (CurrentPageNumbering.Length > 0){
                SingleComposer
                    .GetDynamicText(IDPageArea)
                    .SetNewText(CurrentPageNumbering,false,true,false);
            }
            UpdatingCurrentPageNumbering();
        }

        private bool OnButtonNextPage()
        {
            SavingInputTemporary();
            if (PageCurrent < PageMax)
            {
                PageCurrent += 1;
                UpdatingCurrentPageNumbering();

            }
            SingleComposer
                .GetTextArea(IDTextArea)
                .SetValue(Text[PageCurrent]);
            return true;
        }

        private bool OnButtonPrevPage()
        {
            SavingInputTemporary();
            if (PageCurrent > 0)
            {
                PageCurrent -= 1;
                UpdatingCurrentPageNumbering();
            }
            SingleComposer
                .GetTextArea(IDTextArea)
                .SetValue(Text[PageCurrent]);
            return true;
        }

        private bool OnButtonSub(){
            if (PageMax > 0)
            {
                Text[PageMax] = "";
                PageMax -= 1;
                // Need to return to prev. page if currently displayed lastpage was deleted
                if (PageCurrent > PageMax)
                {
                    SingleComposer
                        .GetTextArea(IDTextArea)
                        .SetValue(Text[PageMax+1]);
                    OnButtonPrevPage();
                    return true;
                }
                
                UpdatingCurrentPageNumbering();
            }
            return true;
        }

        private bool OnButtonAdd()
        {
            if (PageMax < (PageLimit - 1))
            {
                PageMax += 1;
                UpdatingCurrentPageNumbering();

            }
            return true;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (SingleComposer.GetTextArea(IDTextArea) == null){
                SingleComposer.FocusElement(SingleComposer.GetRichtext(IDRichtextArea).TabIndex);
            }
            else {
                SingleComposer.FocusElement(SingleComposer.GetTextArea(IDTextArea).TabIndex);
            }
        }

        private void OnTitleAreaChanged (string value){
            GuiElementTextInput TitleArea = SingleComposer.GetTextInput(IDTitleInput);

            OnTextChanged?.Invoke(TitleArea.GetText());
        }

        private void OnTextAreaChanged(string value)
        {
            GuiElementTextArea TextArea = SingleComposer.GetTextArea(IDTextArea);

            OnTextChanged?.Invoke(TextArea.GetText());
        }


        private void OnTitleBarClose()
        {
            OnButtonCancel();
        }

        // OnButtonSave commits text to block
        private bool OnButtonSave()
        {
            SavingInputTemporary();
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(PageMax);
                for (int i = 0; i <= PageMax; i++)
                {
                    writer.Write(Text[i]);
                }
                writer.Write(Title);
                data = ms.ToArray();
            }
            capi
                .Network
                .SendBlockEntityPacket(BEPos.X, BEPos.Y, BEPos.Z, (int)EnumBookPacketId.SaveBook, data);

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

        public void ReadGui( BlockPos Pos, ICoreClientAPI Capi)
        {
            this.BEPos = Pos;

            ElementBounds
                textAreaBounds = ElementBounds
                    .Fixed(0, 0, WindowWidth, WindowHeight),
                clippingBounds = textAreaBounds
                    .ForkBoundingParent()
                    .WithFixedPosition(0, 30),
                 CancelButtonBounds = ElementBounds
                     .FixedSize(0, 0)
                     .FixedUnder(clippingBounds, 2 * 5)
                     .WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10, 2),
                PageNumberingAreaBounds = ElementBounds
                    .FixedSize(PageNumberingWidth, PageNumberingHeight)
                    .FixedUnder(clippingBounds, 2 * 5)
                    .WithAlignment(EnumDialogArea.RightFixed),
                bgBounds = ElementBounds
                    .Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding),
                dialogBounds = ElementStdBounds
                    .AutosizedMainDialog
                    .WithAlignment(EnumDialogArea.RightMiddle)
                    .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            double textareaFixedY = textAreaBounds.fixedY;

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(clippingBounds, CancelButtonBounds);

            SingleComposer = capi.Gui
                .CreateCompo(CompNameRead, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .BeginClip(clippingBounds)
                        // TODO:
                        .AddRichtext(
                            Text[0], 
                            CairoFont.TextInput().WithFontSize(TextFont),
                            textAreaBounds,
                            IDRichtextArea)
                    .EndClip()
                    .AddSmallButton(Lang.Get(_bClose), OnButtonCancel, CancelButtonBounds)
                    .AddDynamicText(
                        CurrentPageNumbering, 
                        CairoFont.TextInput().WithFontSize(PageNumberingFont), 
                        EnumTextOrientation.Center,
                        PageNumberingAreaBounds,
                        IDPageArea)
                .EndChildElements()
                .Compose()
            ;
        }
    }
}
