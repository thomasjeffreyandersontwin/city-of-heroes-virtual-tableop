using Framework.WPF.Services.MessageBoxService;
using System.Windows;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Always-OK IMessageBoxService — prevents dialog popups during domain tests.
    /// </summary>
    public sealed class FakeMessageBoxService : IMessageBoxService
    {
        public static readonly FakeMessageBoxService Instance = new FakeMessageBoxService();

        private FakeMessageBoxService() { }

        public MessageBoxResult ShowDialog(string message)
        {
            return MessageBoxResult.OK;
        }

        public MessageBoxResult ShowDialog(string message, string caption)
        {
            return MessageBoxResult.OK;
        }

        public MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button)
        {
            return MessageBoxResult.OK;
        }

        public MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button, MessageBoxImage image)
        {
            return MessageBoxResult.OK;
        }
    }
}
