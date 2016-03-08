using System.Collections.Generic;
using EPiServer.Notification;
using EPiServer.ServiceLocation;

namespace EPiServer.Translate
{
    /// <summary>
    /// We are using a BETA API
    /// </summary>
    [ServiceConfiguration(typeof(INotificationFormatter))]
    public class ZapierNotificationFormatter : INotificationFormatter
    {
        public IEnumerable<FormatterNotificationMessage> FormatMessages(
            IEnumerable<FormatterNotificationMessage> notifications, 
            string recipient, 
            NotificationFormat format, 
            string notificationChannelName)
        {
            // we do not want to change the messages, so we just return them as they are
            // but you have the possibility to group several messages into one if you would like to
            return notifications;
        }

        public const string ChannelName = "translate";

        /// <summary>
        /// The name of the formatter
        /// </summary>
        public string FormatterName { get { return "zapire"; } }

        /// <summary>
        /// Specifies which channels the formatter supports.
        /// </summary>
        public IEnumerable<string> SupportedChannelNames
        {
            get { return new[] { ChannelName }; }
        }
    }
}
