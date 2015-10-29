using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Notification;
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
        private ProjectRepository _projectRespository;
        private INotifier _notifier;

        public void Initialize(InitializationEngine context)
        {
            var preferencesRegister = context.Locate.Advanced.GetInstance<INotificationPreferenceRegister>();

            // register the ZapierNotificationProvider to handle all notifications created on the "translate" channel
            preferencesRegister.RegisterDefaultPreference(ZapireNotificationFormatter.ChannelName, ZapireNotificationProvider.Name, s => s);

            // get the dependent services from the IoC container
            _projectRespository = context.Locate.Advanced.GetInstance<ProjectRepository>();
            _notifier = context.Locate.Advanced.GetInstance<INotifier>();
            _contentRepository = context.Locate.Advanced.GetInstance<IContentRepository>();
            _urlResolver = context.Locate.Advanced.GetInstance<EditUrlResolver>();

            // attach an event handler that listens to ProjectItemsSaved events
            ProjectRepository.ProjectItemsSaved += ProjectRepositoryOnProjectItemsSaved;

            // listen to changes made to activities that belongs to a project
            // var projectActivityFeed = context.Locate.Advanced.GetInstance<ProjectActivityFeed>();
            // projectActivityFeed.ActivityCreated += (sender, args) => { };
            // projectActivityFeed.ActivityUpdated += (sender, args) => { };
            // projectActivityFeed.ActivityDeleted += (sender, args) => { };
            
            // Comment events
            // var activityCommentRepository = context.Locate.Advanced.GetInstance<ActivityCommentRepository>();
            // activityCommentRepository.CommentCreated += (sender, args) => { };
            // ...

            // listen to standard episerver content events
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            // contentEvents.CheckedInContent += (sender, args) => { };
            // contentEvents.CheckingInContent += (sender, args) => { };
            // contentEvents.CreatedContent += (sender, args) => { };
            // contentEvents.CreatingContent += (sender, args) => { };
            // contentEvents.DeletedContent += (sender, args) => { };
            // contentEvents.DeletingContent += (sender, args) => { };
            // ...
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
                // get the project from the project repository
                var project = _projectRespository.Get(item.ProjectID);
                
                // continue if the project item does not belong to a project that has our translation marker
                if (!project.Name.Contains("[translate]"))
                    continue;

                // create a new notification and post it
                _notifier.PostNotificationAsync(CreateNotification(project, item)).Wait();
            }
        }

        private NotificationMessage CreateNotification(Project project, ProjectItem item)
        {
            // load the content that is associated with the project item
            var content = _contentRepository.Get<IContent>(item.ContentLink);

            // cast it to change trackable to be able to get the user that changed the item
            var change = content as IChangeTrackable;

            // get the edit link to the content
            var editUrl = _urlResolver.GetEditViewUrl(content.ContentLink, new EditUrlArguments
            {
                ForceEditHost = true, 
                ForceHost = true,
                Language = item.Language
            });

            var changedBy = new NotificationUser(change.ChangedBy);

            // create the notification message
            var notificationMessage = new NotificationMessage
            {
                TypeName = "translate",
                Sender = changedBy,
                Recipients = new[] { changedBy },
                ChannelName = ZapireNotificationFormatter.ChannelName,
                Subject = string.Format("{0} was added to {1}", content.Name, project.Name),
                Content = string.Format("[{0}]({1})", content.Name, editUrl)
            };

            return notificationMessage;
        }
    }
}
