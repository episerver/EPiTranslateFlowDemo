using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Globalization;
using EPiServer.Security;
using EPiServer.Shell.Services.Rest;

namespace EPiServer.Translate
{
    [RestStore("translate")] // Register the store
    public class TranslateStore : RestControllerBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly ProjectRepository _projectRepository;

        public TranslateStore(IContentRepository contentRepository, ProjectRepository projectRepository)
        {
            _contentRepository = contentRepository;
            _projectRepository = projectRepository;
        }

        public ActionResult Translate(ContentReference id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            // get the the current content language
            var preferredContentLanguage = ContentLanguage.PreferredCulture;

            // load the content for the node
            var content = _contentRepository.Get<IContent>(id);

            // get the references to the decendents of id and add the id
            var descendents = _contentRepository.GetDescendents(id);
            
            // create a language selector that fall back to the master language if the current
            // content language can't be found
            var languageSelector = LanguageSelector.Fallback(preferredContentLanguage.Name, true);

            // batch load the data for the descendents            
            var itemsToTranslate = _contentRepository.GetItems(descendents, languageSelector)
                .Union(new[] { content });

            var newContentLinks = new List<ContentReference>();
            foreach (var descendent in itemsToTranslate)
            {
                // cast the content to a localizable resource
                var localizableResource = descendent as ILocalizable;
                if (localizableResource == null)
                    continue;

                // continue if the content already exists in the current content language
                if(localizableResource.ExistingLanguages.Contains(preferredContentLanguage))
                    continue;

                // create a new version in the preferred culture and copy the values
                var translatedContent = _contentRepository.CreateLanguageBranch<IContent>(
                    descendent.ContentLink, 
                    preferredContentLanguage);

                // copy the property values from the master language
                CopyProperties(translatedContent, descendent.Property);

                // set the name of the new item to be the same as the original
                translatedContent.Name = descendent.Name;

                // save the new item, and make sure that the user has Create access
                var newRef = _contentRepository.Save(translatedContent, SaveAction.Save | SaveAction.SkipValidation, AccessLevel.Create);

                // add the new reference to the list of content links
                newContentLinks.Add(newRef);
            }

            // return  Conflict (409) if there were no items translated 
            if (!newContentLinks.Any())
                return new RestStatusCodeResult((int)HttpStatusCode.Conflict);

            // create a new project
            var project = new Project
            {
                Name = string.Format("{0}({1})", content.Name, preferredContentLanguage)
            };

            // create the project and add a project item for each content that has been translated
            _projectRepository.Save(project);

            // create a ProjectItem for each content link that was created earlier
            var projectItems = newContentLinks.Select(contentLink => new ProjectItem
            {
                Category = "translate",
                ProjectID = project.ID,
                ContentLink = contentLink,
                Language = preferredContentLanguage
            });

            // save the new project items
            _projectRepository.SaveItems(projectItems);
            
            // return the new project id to the client
            return Rest(project.ID);
        }

        private void CopyProperties(IContentData content, PropertyDataCollection properties)
        {
            foreach (var property in properties)
            {
                // continue if the property isn't languagespecific or a metadata property
                if (!content.Property[property.Name].IsLanguageSpecific || content.Property[property.Name].IsMetaData)
                {
                    continue;
                }
                // if it is a block, recursively copy the property values
                if (property.Value is IContentData)
                {
                    CopyProperties(content.Property[property.Name].Value as IContentData, (property.Value as IContentData).Property);
                }
                else
                {
                    // copy the property value
                    content.Property[property.Name].Value = property.Value;
                }
            }
        }
    }
}