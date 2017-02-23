var servicesModule = angular.module('admin.services', []);
(function (window, angular, undefined) {
    'use strict';

    servicesModule.factory('dashboardData', dashboardData);

    dashboardData.$inject= ['$http'];

    function dashboardData($http) {
        var service = {
            getApplicationStats: getApplicationStats
        };

        return service;

        function getApplicationStats(appName) {
            return $http.get('/' + appName + '/status').then(function (d) {

                return d.data;
            });
        }
    }

})(window, window.angular);
