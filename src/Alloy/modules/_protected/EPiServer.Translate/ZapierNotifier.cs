using System;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Notification;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace EPiServer.Translate
{
    /// <summary>
    /// Create a module that get initialized after the CMS Module has been initialized
    /// </summary>
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ZapierNotifier : IInitializableModule
    {
        private IContentRepository _contentRepository;
        private EditUrlResolver _urlResolver;
        private ProjectRepository _projectRepository;
        private INotifier _notifier;

        public void Initialize(InitializationEngine context)
        {
            var serviceLocator = context.Locate.Advanced;

            // get the dependent services from the IoC container
            ResolveDependencies(serviceLocator);

            // register the ZapierNotificationProvider to handle all notifications created on the "translate" channel
            RegisterNotificationProvider(serviceLocator);

            // attach an event handler that listens to ProjectItemsSaved events
            ProjectRepository.ProjectItemsSaved += ProjectRepositoryOnProjectItemsSaved;
        }

        public void Uninitialize(InitializationEngine context)
        {
            // remove the event handler when the modules gets un initialized
            ProjectRepository.ProjectItemsSaved -= ProjectRepositoryOnProjectItemsSaved;
        }

        private void ProjectRepositoryOnProjectItemsSaved(object sender, ProjectItemsEventArgs e)
        {
            foreach (var item in e.ProjectItems)
            {
                // continue if the project item does not belong to a project that has our translation marker
                if (!item.Category.Equals("translate", StringComparison.OrdinalIgnoreCase))
                    continue;

                // get the project from the project repository
                var project = _projectRepository.Get(item.ProjectID);
                
                // create a new notification and post it
                _notifier.PostNotificationAsync(CreateNotification(project, item)).Wait();
            }
        }

        private NotificationMessage CreateNotification(Project project, ProjectItem item)
        {
            // load the content that is associated with the project item
            var content = _contentRepository.Get<IContent>(item.ContentLink);

            // get the edit link to the content
            var editUrl = _urlResolver.GetEditViewUrl(content.ContentLink, new EditUrlArguments
            {
                ForceEditHost = true, 
                ForceHost = true,
                Language = item.Language
            });

            // cast it to change trackable to be able to get the user that changed the item
            var change = content as IChangeTrackable;
            var changedBy = new NotificationUser(change.ChangedBy);

            // create the notification message
            var notificationMessage = new NotificationMessage
            {
                TypeName = "translate",
                Sender = changedBy,
                Recipients = new[] { changedBy },
                ChannelName = ZapierNotificationFormatter.ChannelName,
                Subject = string.Format("{0} was added to {1}", content.Name, project.Name),
                Content = string.Format("[{0}]({1})", content.Name, editUrl)
            };

            return notificationMessage;
        }

        private void RegisterNotificationProvider(IServiceLocator serviceLocator)
        {
            var preferencesRegister = serviceLocator.GetInstance<INotificationPreferenceRegister>();

            // register the ZapierNotificationProvider to handle all notifications created on the "translate" channel
            preferencesRegister.RegisterDefaultPreference(
                ZapierNotificationFormatter.ChannelName,
                ZapierNotificationProvider.Name,
                s => s);
        }

        private void ResolveDependencies(IServiceLocator serviceLocator)
        {

            _projectRepository = serviceLocator.GetInstance<ProjectRepository>();
            _notifier = serviceLocator.GetInstance<INotifier>();
            _contentRepository = serviceLocator.GetInstance<IContentRepository>();
            _urlResolver = serviceLocator.GetInstance<EditUrlResolver>();
        }
    }
}
