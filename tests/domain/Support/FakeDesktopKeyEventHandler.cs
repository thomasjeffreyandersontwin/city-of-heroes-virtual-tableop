using Module.HeroVirtualTabletop.Desktop;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// No-op IDesktopKeyEventHandler — prevents global keyboard hooks from activating during domain tests.
    /// </summary>
    public sealed class FakeDesktopKeyEventHandler : IDesktopKeyEventHandler
    {
        public static readonly FakeDesktopKeyEventHandler Instance = new FakeDesktopKeyEventHandler();

        private FakeDesktopKeyEventHandler() { }

        public void AddKeyEventHandler(HandleKeyEvent handleKeyEvent) { }
        public void RemoveKeyEventHandler(HandleKeyEvent handleKeyEvent) { }
    }
}
