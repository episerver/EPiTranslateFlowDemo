define([
    "dojo/_base/lang",
    "epi-cms/component/ContentContextMenuCommandProvider",
    "./commands/TranslateNodes"
], function (
    lang,
    ContentContextMenuCommandProvider,
    TranslateNodes
) {
    // REMARKS:
    // This is not the intended way of extending our command provider for the context menus in the tree
    // We are working on a better way that does not include replacing our methods

    var originalMethod = ContentContextMenuCommandProvider.prototype.postscript;

    // Replace the original postscript method with our own
    lang.mixin(ContentContextMenuCommandProvider.prototype, {

        postscript: function () {
            // Execute the original postscript method
            originalMethod.call(this);

            //Create our TranslateNodes command
            var translateCommand = new TranslateNodes(this._settings);

            // Add the commands to the list of available commands
            this.commands.push(translateCommand);
        }
    });

    ContentContextMenuCommandProvider.prototype.postscript.nom = "postscript";
});