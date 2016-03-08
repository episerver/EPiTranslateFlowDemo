define([
    "dojo/_base/declare",

    "epi/_Module",
    "epi/routes",
    "./ContextMenuCommandProvider" // Load our new ContextMenuProvider
], function (declare, _Module, routes) {

    return declare([_Module], {
        initialize: function () {
            // summary:
            //		Initialize module

            // Execute the base initialize code first
            this.inherited(arguments);

            // Get the store registry from the dependency container
            var registry = this.resolveDependency("epi.storeregistry");

            // Get the route for the server side store and create a REST JS store
             registry.create(
                "epi-translate.translate",
                routes.getRestPath({
                    moduleArea: "EPiServer.Translate",
                    storeName: "translate"
                })
             );
        }
    });

});