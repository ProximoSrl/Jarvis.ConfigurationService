(function (window, angular, undefined) {
    'use strict';

    angular
        .module('admin')
        .config(config);

    /**/ 
    function config($stateProvider, $urlRouterProvider) {
        //
        // For any unmatched url, redirect to /state1
        $urlRouterProvider.otherwise("/dashboard");
        //
        // Now set up the states
        $stateProvider
            .state('dashboard', {
                url: "/dashboard",
                templateUrl: "dashboard/dashboard.html",
                controller: "DashboardController as dashboard",
                data: { pageTitle: 'Dashboard'},
                resolve: {
                    configService : 'configService',
                    status: function (configService) {
                        return configService.getStatus();
                    }
                }
            });
    }
})(window, window.angular);
