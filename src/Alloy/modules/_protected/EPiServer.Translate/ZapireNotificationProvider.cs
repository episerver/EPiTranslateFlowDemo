using EPiServer.Notification;
using EPiServer.ServiceLocation;
using RestSharp;
using System;
using System.Collections.Generic;

namespace EPiServer.Translate
{
    /// <summary>
    /// We are using a BETA API
    /// </summary>
    [ServiceConfiguration(typeof(INotificationProvider))]
    public class ZapireNotificationProvider: INotificationProvider
    {
        private readonly RestClient _client;

        public ZapireNotificationProvider()
        {
            // create a rest client and use the url provided by Zapier
            _client = new RestClient("https://zapier.com/hooks/catch/3t0dam/");
        }

        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public string ProviderName
        {
            get { return "zapier"; }
        }

        /// <summary>
        /// Specifies the format the provider supports.
        /// </summary>
        public NotificationFormat GetProviderFormat()
        {
            return new NotificationFormat { MaxLength = null, SupportsHtml = false };
        }
        /// <summary>
        /// Sends the formatted messages.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        /// <param name="succeededAction">A success action that should be called for successfully sent messages.</param>
        /// <param name="failedAction">A failure action that should be called when a message send operation fails.</param>
        public void Send(IEnumerable<ProviderNotificationMessage> messages, Action<ProviderNotificationMessage> succeededAction, Action<ProviderNotificationMessage, Exception> failedAction)
        {
            foreach (var message in messages)
            {
                try
                {
                    // create a new rest request and add the message to the http message body
                    var request = new RestRequest(Method.POST);
                    request.AddJsonBody(message);

                    // send the POST request and call success action if everything went fine
                    _client.Post<ProviderNotificationMessage>(request);
                    if (succeededAction != null)
                        succeededAction(message);
                }
                catch(Exception e)
                {
                    // if the post fails call the failedAction 
                    if(failedAction != null)
                        failedAction(message, e);
                }
            }
        }
    }
}
