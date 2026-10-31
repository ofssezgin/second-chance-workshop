using System.Collections.Generic;
using CopAtolyesi.Core;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Arayuz parcalarinin ortak ihtiyaci: oturum ve her seyin ustunde duran katman.
    public sealed class UiContext
    {
        public GameSession Session;
        public RectTransform Overlay;
        public TooltipView Tooltip;
        public ToastView Toast;
        public WashStation Wash;
        public ItemGridView BackShelf;
        public ItemGridView FrontShelf;
        public CraftTableView Craft;
        public SummaryView Summary;
        public FinalView Final;
        public PauseView Pause;
        public SettingsView Settings;
        public ConfirmView Confirm;
        public NoticeView Notice;
        public DeliveryZone Delivery;
        public OrderReminderView Reminder;
        public NotebookView Notebook;
        public TutorialView Tutorial;
        public readonly Dictionary<string, RectTransform> Targets = new Dictionary<string, RectTransform>();
    }
}
