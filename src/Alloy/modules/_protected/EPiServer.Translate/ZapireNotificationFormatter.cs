using System.Collections.Generic;
using EPiServer.Notification;
using EPiServer.ServiceLocation;

namespace EPiServer.Translate
{
    /// <summary>
    /// We are using a BETA API
    /// </summary>
    [ServiceConfiguration(typeof(INotificationFormatter))]
    public class ZapireNotificationFormatter : INotificationFormatter
    {
        public IEnumerable<FormatterNotificationMessage> FormatMessages(IEnumerable<FormatterNotificationMessage> notifications, string sender, string recipient, NotificationFormat format, string notificationChannelName)
        {
            // we do not want to change the messages, so we just return them as they are
            return notifications;
        }

        /// <summary>
        /// The name of the formatter
        /// </summary>
        public string FormatterName { get { return "zapire"; } }

        /// <summary>
        /// Specifies which channels the formatter supports.
        /// </summary>
        public IEnumerable<string> SupportedChannelNames
        {
            get { return new[] {"translate"}; }
        }
    }
}
