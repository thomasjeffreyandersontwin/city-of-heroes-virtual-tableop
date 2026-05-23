using Framework.WPF.Services.BusyService;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// No-op IBusyService — prevents busy-animation calls from throwing during domain tests.
    /// </summary>
    public sealed class FakeBusyService : IBusyService
    {
        public static readonly FakeBusyService Instance = new FakeBusyService();

        private FakeBusyService() { }

        public bool IsShowingBusy { get { return false; } }

        public void ShowBusy() { }
        public void ShowBusy(string text) { }
        public void ShowBusy(string[] windowNames) { }
        public void HideBusy() { }
        public void HideAllBusy() { }
    }
}
