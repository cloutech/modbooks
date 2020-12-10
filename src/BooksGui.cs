using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

// TODO: nicer background!
// TODO: onRead send TreeAttributes of arText only (decrease networking)
// Feat1: Help-Tab for book, shows more info on format, features etc.
// Feat2: Add waypoint sharing support?

namespace books.src
{
    class BooksGui : GuiDialogGeneric
    {
        ICoreClientAPI Capi;

        private int 
            PageCurrent = 0;

        public int 
            PageMax { get; private set; }

        private static int
            MaxTitleWidth = 240,
            MaxLines = 18,
            MaxWidth = 580,
            // PageLimit, how many pages players can have in a book,
            // network performance impact on chunk load and player join
            PageLimit = 20,
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

        private string
            Title = "",
            CurrentPageNumbering = "1/1",
            EditTitle = "",
            // button texts:
            _bCancel = "",
            _bSave = "",
            _bClose = "",
            _bHelp = "";


        private static string
            // Language en.json references:
            LangTextDef = "books:editor-text-default",
            LangTitelDef = "books:editor-titel-default",
            LangTitelEditor = "books:editor-titel",
            LangbCancel = "books:editor-cancel",
            LangbSave = "books:editor-save",
            LangbClose = "books:editor-close",
            LangbHelp = "books:editor-help",
            //LangHelpText = "books:editor-help-text",
            // IDs/dialog keys:
            DialogNameEditor = "bookeditor",
            CompNameRead = "blockentitytextreaddialog",
            CompNameEdit = "blockentitytexteditordialog",
            IDTextArea = "text",
            IDRichtextArea = "page",
            IDTitleInput = "title",
            IDPageArea = "page-numbering",
            //IDHelpArea = "help-page",
            _bSub = "-",
            _bAdd = "+",
            _bNextPage = ">>",
            _bPrevPage = "<<";

        private string[] Text = new string[20];

        BlockPos BEPos;

        public Action<string> OnTextChanged;
        public Action OnCloseCancel;

        public bool 
            didSave,
            isPaper = false,
            Unique = false;

        public BooksGui(bool unique, string booktitle, string[] text, int pagemax, ICoreClientAPI capi, string dialogTitel) : base(dialogTitel, capi)
        {
            Capi = capi;
            GetLangEntries();
            PageMax = pagemax;
            DeletingText();
            text.CopyTo(Text, 0);
            Title = booktitle;
            Unique = unique;
        }
        public BooksGui(bool isPaper, bool unique, string booktitle, string[] text, int pagemax, ICoreClientAPI capi, string dialogTitel) : base(dialogTitel, capi)
        {
            Capi = capi;
            GetLangEntries();
            this.isPaper = isPaper;
            PageMax = pagemax;
            DeletingText();
            text.CopyTo(Text, 0);
            Title = booktitle;
            Unique = unique;
        }

        public BooksGui(bool unique, string booktitle, string[] text, int pagemax, BlockPos BlockEntityPosition, ICoreClientAPI capi, string dialogTitel) : base(dialogTitel, capi)
        {
            Capi = capi;
            BEPos = BlockEntityPosition;
            GetLangEntries();
            Title = booktitle;
            PageMax = pagemax;
            text.CopyTo(Text, 0);
            Unique = unique;
        }

        private void GetLangEntries()
        {
            EditTitle = Lang.Get(LangTitelEditor);
            _bCancel = Lang.Get(LangbCancel);
            _bSave = Lang.Get(LangbSave);
            _bClose = Lang.Get(LangbClose);
            _bHelp = Lang.Get(LangbHelp);
        }

        private void DeletingText()
        {
            Unique = false;
            for (int i = 0; i < PageLimit; i++)
            {
                Text[i] = "";
            }
            Text[0] = Lang.Get(LangTextDef);
            Title = Lang.Get(LangTitelDef);
        }

        private void UpdatingText()
        {
            if (DialogTitle == DialogNameEditor)
            {
                Composers[CompNameEdit]
                    .GetTextArea(IDTextArea)
                     .SetValue(Text[PageCurrent]);
            }
            else {
                // to keep richtext functionality
                ReadGui(this.BEPos, this.Capi);
            }
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
            temp_page = PageMax;
            lastPage = temp_page.ToString();

            updatedCurrentPageNumbering = string.Concat(
                currentPage,
                dividerLayout,
                lastPage
                );

            CurrentPageNumbering = updatedCurrentPageNumbering.ToString();
            if (DialogTitle == DialogNameEditor)
            {
                Composers[CompNameEdit]
                   .GetDynamicText(IDPageArea)
                    .SetNewText(CurrentPageNumbering, false, true, false);
            }
            else {
                Composers[CompNameRead]
                    .GetDynamicText(IDPageArea)
                        .SetNewText(CurrentPageNumbering, false, true, false);
            }
        }

        private bool SavingInputTemporary()
        {
            if (DialogTitle == DialogNameEditor)
            {
                Title = Composers[CompNameEdit]
                    .GetTextInput(IDTitleInput)
                    .GetText();
                Text[PageCurrent] = Composers[CompNameEdit]
                    .GetTextArea(IDTextArea)
                    .GetText();
            }
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
                    .WithFixedAlignmentOffset(((WindowWidth / 2) + 10), 0)
                    .WithFixedPadding(3, 2),
                SubPageButtonBounds = ElementBounds
                    .FixedSize(0, 0)
                    .FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(((WindowWidth / 2) - 10), 0)
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

            //flag_RW = flag_W;

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

            Composers[CompNameEdit] = capi.Gui
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
                .Compose();


            Composers[CompNameEdit]
                .GetTextArea(IDTextArea)
                    .SetMaxLines(MaxLines);
            Composers[CompNameEdit]
                .GetTextArea(IDTextArea)
                    .SetMaxWidth((int)(MaxWidth * RuntimeEnv.GUIScale));
            Composers[CompNameEdit]
                .GetTextInput(IDTitleInput)
                    .SetMaxWidth(MaxTitleWidth);

            if (Text.Length > 0)
            {
                Composers[CompNameEdit]
                  .GetTextArea(IDTextArea)
                    .SetValue(Text[PageCurrent]);
            }
            if (Title.Length > 0)
            {
                Composers[CompNameEdit]
                    .GetTextInput(IDTitleInput)
                        .SetValue(Title);
            }
            if (CurrentPageNumbering.Length > 0)
            {
                Composers[CompNameEdit]
                    .GetDynamicText(IDPageArea)
                        .SetNewText(CurrentPageNumbering, false, true, false);
            }
            UpdatingCurrentPageNumbering();
        }

        private bool OnButtonNextPage()
        {
            if (PageCurrent < (PageMax - 1))
            {
                SavingInputTemporary();
                PageCurrent += 1;
                UpdatingText();
                UpdatingCurrentPageNumbering();

            }
            return true;
        }

        private bool OnButtonPrevPage()
        {
            if (PageCurrent > 0)
            {
                SavingInputTemporary();
                PageCurrent -= 1;
                UpdatingText();
                UpdatingCurrentPageNumbering();

            }
            return true;
        }

        private bool OnButtonSub()
        {

            if (PageMax > 1)
            {
                Text[PageMax] = "";
                PageMax -= 1;
            }
            // Need to return to prev. page if currently displayed lastpage was deleted
            if (PageCurrent >= PageMax)
            {
                UpdatingText();
                OnButtonPrevPage();
            }
            UpdatingCurrentPageNumbering();

            return true;
        }

        private bool OnButtonAdd()
        {
            if(isPaper)
            {
                if (PageMax == 2)
                    return true; 
            }

            if (PageMax < PageLimit)
            {
                PageMax += 1;
                Text[PageMax-1] = "";
                UpdatingCurrentPageNumbering();
            }

            return true;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (DialogTitle == DialogNameEditor)
            {
                Composers[CompNameEdit]
                    .FocusElement(Composers[CompNameEdit]
                    .GetTextArea(IDTextArea)
                    .TabIndex);
            }
            else {
                Composers[CompNameRead]
                    .FocusElement(Composers[CompNameRead]
                    .GetRichtext(IDRichtextArea)
                    .TabIndex);
            }
        }

        private void OnTitleAreaChanged(string value)
        {
            if (DialogTitle == DialogNameEditor)
            {
                GuiElementTextInput TitleArea = Composers[CompNameEdit].GetTextInput(IDTitleInput);

                OnTextChanged?.Invoke(TitleArea.GetText());
            }
        }

        private void OnTextAreaChanged(string value)
        {
            if (DialogTitle == DialogNameEditor)
            {
                GuiElementTextArea TextArea = Composers[CompNameEdit].GetTextArea(IDTextArea);

                OnTextChanged?.Invoke(TextArea.GetText());
            }
        }


        private void OnTitleBarClose()
        {
            OnButtonCancel();
        }

        private bool OnButtonSave()
        {
            // OnButtonSave commits text to block
            // making it unique
            if (DialogTitle == DialogNameEditor)
            {
                SavingInputTemporary();
                Unique = true;

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(PageMax);
                    for (int i = 0; i < PageMax; i++)
                    {
                        writer.Write(Text[i]);
                    }
                    writer.Write(Title);
                    writer.Write(Unique);
                    data = ms.ToArray();
                }
                capi
                    .Network
                    .SendBlockEntityPacket(BEPos.X, BEPos.Y, BEPos.Z, (int)EnumBookPacketId.SaveBook, data);

                didSave = true;
                TryClose();
                Dispose(); 
            }
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

        // TODO: ReadGui
        // Need of implementation: only send data of individual page on reading
        // not on chunk load/player join!
        // reduce network traffic
        // only send book title info
        // //////>> populate BooksNetworkHandler
        // see: GuiDialogJournal.Paginate
        public void ReadGui(BlockPos Pos, ICoreClientAPI Capi)
        {
            this.BEPos = Pos;

            ElementBounds
                textAreaBounds = ElementBounds
                    .Fixed(0, 0, WindowWidth, WindowHeight),
                ClippingBounds = textAreaBounds
                    .ForkBoundingParent()
                    .WithFixedPosition(0, 30),
                CancelButtonBounds = ElementBounds
                    .FixedSize(0, 0).FixedUnder(ClippingBounds, 2 * 5)
                    .WithFixedAlignmentOffset(0, 0)
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
                    .Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding),
                dialogBounds = ElementStdBounds
                    .AutosizedMainDialog
                    .WithAlignment(EnumDialogArea.RightMiddle)
                    .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            double textareaFixedY = textAreaBounds.fixedY;

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(
                ClippingBounds,
                CancelButtonBounds,
                NextPageButtonBounds,
                PrevPageButtonBounds,
                PageNumberingAreaBounds);

            Composers[CompNameRead] = capi.Gui
                .CreateCompo(CompNameRead, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Title, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .BeginClip(ClippingBounds)
                        // TODO:
                        .AddRichtext(
                            Text[PageCurrent],
                            CairoFont.TextInput().WithFontSize(TextFont),
                            textAreaBounds,
                            IDRichtextArea)
                    .EndClip()
                    .AddSmallButton(Lang.Get(_bClose), OnButtonCancel, CancelButtonBounds)
                    .AddSmallButton(Lang.Get(_bNextPage), OnButtonNextPage, NextPageButtonBounds)
                    .AddSmallButton(Lang.Get(_bPrevPage), OnButtonPrevPage, PrevPageButtonBounds)
                    .AddDynamicText(
                        CurrentPageNumbering,
                        CairoFont.TextInput().WithFontSize(PageNumberingFont),
                        EnumTextOrientation.Center,
                        PageNumberingAreaBounds,
                        IDPageArea)
                .EndChildElements()
                .Compose();

            UpdatingCurrentPageNumbering();
        }
    }
}
