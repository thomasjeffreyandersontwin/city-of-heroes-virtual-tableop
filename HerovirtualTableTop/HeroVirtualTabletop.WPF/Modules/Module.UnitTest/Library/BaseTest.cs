using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Moq;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Module.UnitTest
{
    public class BaseTest
    {
        /// <summary>Clears KeyBindsGenerator static accumulation between tests (call from each suite's TestInitialize).</summary>
        protected static void ResetKeyBindGeneratorStatics()
        {
            var t = typeof(KeyBindsGenerator);
            var buf = t.GetField("generatedKeybindText", BindingFlags.NonPublic | BindingFlags.Static);
            if (buf != null)
                buf.SetValue(null, string.Empty);
            var last = t.GetField("lastKeyBindGenerated", BindingFlags.NonPublic | BindingFlags.Static);
            if (last != null)
                last.SetValue(null, null);
            var listField = t.GetField("generatedKeybinds", BindingFlags.NonPublic | BindingFlags.Static);
            if (listField != null)
            {
                var list = listField.GetValue(null) as List<string>;
                if (list != null)
                    list.Clear();
            }
            Camera.ResetStaticsForUnitTests();
        }

        protected Mock<IBusyService> busyServiceMock = new Mock<IBusyService>();
        protected Mock<IPopupService> popupServiceMock = new Mock<IPopupService>();
        /// <summary>Prism EventAggregator has non-virtual GetEvent; tests use a real instance.</summary>
        protected readonly EventAggregator eventAggregator = new EventAggregator();
        protected Mock<IMessageBoxService> messageBoxServiceMock = new Mock<IMessageBoxService>();
        protected Mock<IUnityContainer> unityContainerMock = new Mock<IUnityContainer>();
    }
}
