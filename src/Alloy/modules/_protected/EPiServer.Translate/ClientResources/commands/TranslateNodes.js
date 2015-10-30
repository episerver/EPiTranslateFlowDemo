define([
    "dojo/_base/declare",
    "dojo/topic",
    // Parent class and mixins
    "epi/dependency",
    "epi/shell/command/_Command",
    "epi/shell/command/_SelectionCommandMixin"
], function (
    declare,
    topic,
    // Parent class and mixins
    dependency,
    _Command,
    _SelectionCommandMixin
) {

    return declare([_Command, _SelectionCommandMixin], {

        label: "Translate nodes",

        // Set the icon for the command
        iconClass: "epi-iconWebsite", //http://ux.episerver.com/#icons

        postscript: function () {
            this.inherited(arguments);

            // get the store from the store registry
            this.translateStore = this.translateStore || dependency.resolve("epi.storeregistry").get("epi-translate.translate");
        },

        _execute: function () {
            var selectionData = this.get("selectionData"),
                sender = this;
            var contentLink = selectionData.contentLink;

            this.translateStore.executeMethod("Translate", contentLink).then(function (projectId) {
                // reload the children of the selected item
                topic.publish("/epi/cms/contentdata/childrenchanged", selectionData);
                // reload the selected item
                topic.publish("/epi/cms/contentdata/updated", selectionData);

                // load the project overview for the newly created project
                topic.publish("/epi/shell/context/request", { uri: "epi.cms.project:///" + projectId }, { sender: sender });
            }).otherwise(function() {
                console.log("could not translate the nodes");
            });
        },

        _onModelChange: function () {
            // enable the command if there is a selection in the tree
            this.set("canExecute", !!this.get("selectionData"));
        }
    });
});