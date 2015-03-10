(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').factory('dashboardData', dashboardData);

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
